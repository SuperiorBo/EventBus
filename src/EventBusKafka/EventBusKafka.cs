using EventBus.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using EventBus;
using EventBus.Events;
using Newtonsoft.Json;
using Autofac;
using Confluent.Kafka;
using EventBus.Extensions;
using Microsoft.Extensions.Logging;
using Polly;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace EventBus.Kafka
{
    public class EventBusKafka : IEventBus, IDisposable
    {
        const int commitPeriod = 1;

        private readonly DefaultKafkaProducerConnection<Null, byte[]> _kafkaProducerConnection;
        private readonly DefaultKafkaConsumerConnection<Null, byte[]> _kafkaConsumerConnection;
        private readonly IEventStore _eventStore;
        private readonly ILogger<EventBusKafka> _logger;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "kafka_event_bus";
        private readonly int _retryCount;
        private IConsumer<Null, byte[]> _consumer;

        public EventBusKafka(
            DefaultKafkaProducerConnection<Null, byte[]> kafkaProducerConnection,
            DefaultKafkaConsumerConnection<Null, byte[]> kafkaConsumerConnection,
            IEventStore eventStore,
            ILogger<EventBusKafka> logger,
            ILifetimeScope autofac,
            int retryCount = 5
        )
        {
            _kafkaProducerConnection = kafkaProducerConnection;
            _kafkaConsumerConnection = kafkaConsumerConnection;
            _eventStore = eventStore ?? new EventStoreInMemory();
            _logger = logger;
            _autofac = autofac;
            _retryCount = retryCount;
            _eventStore.OnEventRemoved += EventStore_OnEventRemoved;
            _consumer = CreateConsumer();
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEventBase
        {
            if (!_kafkaProducerConnection.IsConnected)
                _kafkaProducerConnection.TryConnect();

            var policy = Policy.Handle<SocketException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = @event.GetType().Name;

            _logger.LogTrace("Creating Kafka producer to publish event: {EventId} ({EventName})", @event.EventId, eventName);

            

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            using (var producer = _kafkaProducerConnection.CreateConnect() as IProducer<Null, byte[]>)
            {
                policy.Execute(() =>
                {
                    _logger.LogTrace("Publishing event to Kafka: {EventId}", @event.EventId);

                    producer.Produce(
                        eventName,
                        new Message<Null, byte[]>
                        {
                            Value = body
                        });
                });
            }
        }

        public void Subscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = _eventStore.GetEventKey<TEvent>();

            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TEventHandler).GetGenericTypeName());

            DoInternalSubscription(eventName);

            _eventStore.AddSubscription<TEvent,TEventHandler>();

            StartBasicConsume();
        }

        public void SubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TEventHandler).GetGenericTypeName());

            DoInternalSubscription(eventName);

            _eventStore.AddDynamicSubscription<TEventHandler>(eventName);

            StartBasicConsume();
        }

        public void Unsubscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            _eventStore.RemoveSubscription<TEvent, TEventHandler>();
        }

        public void UnsubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

            _eventStore.RemoveDynamicSubscription<TEventHandler>(eventName);
        }
        public void Dispose()
        {
            _kafkaConsumerConnection?.Dispose();
            _kafkaProducerConnection?.Dispose();
            _eventStore.Clear();
        }

        #region Private Method

        private void EventStore_OnEventRemoved(object sender, string eventName)
        {
            if (!_kafkaConsumerConnection.IsConnected)
            {
                _kafkaConsumerConnection.TryConnect();
            }
            
            if (_kafkaConsumerConnection.CreateConnect() is IConsumer<Null,byte[]> consumer)
            {
                var subscription = consumer.Subscription;
                subscription.Remove(eventName);
                consumer.Subscribe(subscription);
            }
            else
                _logger.LogError("Kafka basic consumer is null");
        }

        public void StartBasicConsume()
        {
            _logger.LogTrace("Starting Kafka basic consume");

            if (_kafkaConsumerConnection != null)
            {
                _kafkaConsumerConnection.OnMessageReceived += Consumer_Received;

                _kafkaConsumerConnection.Consume();
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _kafkaConsumerConnection == null");
            }
        }

        private void DoInternalSubscription(string eventName)
        {
            var containKey = _eventStore.HasSubscriptionsForEvent(eventName);
            if (!containKey)
            {
                if (!_kafkaConsumerConnection.IsConnected)
                    _kafkaConsumerConnection.TryConnect();

                if (_kafkaConsumerConnection.CreateConnect() is IConsumer<Null, byte[]> consumer)
                {
                    var subscription = consumer.Subscription;
                    subscription.Add(eventName);
                    consumer.Subscribe(subscription);
                }
                else
                {
                    _logger.LogError("DoInternalSubscription can't call on consumer == null");
                }
            }
        }

        private void Consumer_Received(object sender, ConsumeResult<Null, byte[]> eventArgs)
        {
            var eventName = eventArgs.Topic;
            var message = Encoding.UTF8.GetString(eventArgs.Value);

            try
            {
                ProcessEvent(eventName, message).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }

            if (eventArgs.Offset % commitPeriod == 0)
            {
                // The Commit method sends a "commit offsets" request to the Kafka
                // cluster and synchronously waits for the response. This is very
                // slow compared to the rate at which the consumer is capable of
                // consuming messages. A high performance application will typically
                // commit offsets relatively infrequently and be designed handle
                // duplicate messages in the event of failure.
                try
                {
                    _consumer.Commit(eventArgs);
                }
                catch (KafkaException e)
                {
                    _logger.LogError($"Commit error: {e.Error.Reason}");
                }
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace("Processing Kafka event: {EventName}", eventName);


            if (_eventStore.HasSubscriptionsForEvent(eventName))
            {
                using var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME);

                var subscriptions = _eventStore.GetHandlersForEvent(eventName);

                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicEventHandler;
                        if (handler == null) continue;
                        dynamic eventData = JObject.Parse(message);

                        await handler.Handle(eventData);
                    }
                    else
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        if (handler == null) continue;

                        var eventType = _eventStore.GetEventTypeByName(eventName);

                        var eventData = JsonConvert.DeserializeObject(message, eventType);
                        var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { eventData });
                    }
                }
            }
            else
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            }
        }

        private IConsumer<Null, byte[]> CreateConsumer()
        {
            if (!_kafkaConsumerConnection.IsConnected)
                _kafkaConsumerConnection.TryConnect();

            _consumer = _kafkaConsumerConnection.CreateConnect() as IConsumer<Null, byte[]>;

            _kafkaConsumerConnection.CallCanceledException += (sender, e) =>
            {
                _logger.LogCritical($"Closing consumer. error: {e.Message}");
                _consumer.Close();
                _consumer = CreateConsumer();
                StartBasicConsume();
            };

            return _consumer;
        }

        #endregion
    }
}
