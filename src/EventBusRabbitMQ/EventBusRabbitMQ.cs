using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using EventBus;
using EventBus.Abstractions;
using EventBus.Events;
using EventBus.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBusRabbitMQ
{
    public class EventBusRabbitMQ:IEventBus,IDisposable
    {
        const string BROKER_NAME = "default_event_bus";

        private readonly IRabbitMQConnection _rabbitMQConnection;
        private readonly IEventStore _eventStore;
        private readonly ILogger<EventBusRabbitMQ> _logger;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "default_event_bus";
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private string _queueName;

        public EventBusRabbitMQ(
            IRabbitMQConnection rabbitMQConnection,
            IEventStore eventStore,
            ILogger<EventBusRabbitMQ> logger,
            ILifetimeScope autofac,
            string queueName = null,
            int retryCount = 5
        )
        {
            _rabbitMQConnection = rabbitMQConnection;
            _eventStore = eventStore ?? new EventStoreInMemory();
            _logger = logger;
            _autofac = autofac;
            _queueName = queueName;
            _retryCount = retryCount;
            _consumerChannel = CreateConsumerChannel();
            _eventStore.OnEventRemoved += EventStore_OnEventRemoved;
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEventBase
        {
            if (!_eventStore.HasSubscriptionsForEvent<TEvent>())
                throw new ArgumentException("");

            if (!_rabbitMQConnection.IsConnected)
                _rabbitMQConnection.TryConnect();

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = @event.GetType().Name;

            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.EventId, eventName);

            using var channel = _rabbitMQConnection.CreateModel();

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.EventId);

                channel.BasicPublish(
                    exchange: BROKER_NAME,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

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
            _logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TEventHandler).GetGenericTypeName());

            DoInternalSubscription(eventName);

            _eventStore.AddDynamicSubscription<TEventHandler>(eventName);
            
            StartBasicConsume();
        }

        public void Unsubscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            _eventStore.RemoveSubscription<TEvent,TEventHandler>();
        }

        public void UnsubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

            _eventStore.RemoveDynamicSubscription<TEventHandler>(eventName);
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();
            _eventStore.Clear();
        }

        #region Private Method

        private void EventStore_OnEventRemoved(object sender, string eventName)
        {
            if (!_rabbitMQConnection.IsConnected)
            {
                _rabbitMQConnection.TryConnect();
            }

            using var channel = _rabbitMQConnection.CreateModel();

            channel.QueueUnbind(queue: _queueName,
                exchange: BROKER_NAME,
                routingKey: eventName);
        }

        private void DoInternalSubscription(string eventName)
        {
            var containKey = _eventStore.HasSubscriptionsForEvent(eventName);
            if (!containKey)
            {
                if (!_rabbitMQConnection.IsConnected)
                    _rabbitMQConnection.TryConnect();

                using var channel = _rabbitMQConnection.CreateModel();

                channel.QueueBind(queue: _queueName,
                                  exchange: BROKER_NAME,
                                  routingKey: eventName);
            }
        }

        private void StartBasicConsume()
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }

        }

        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);


            if (_eventStore.HasSubscriptionsForEvent(eventName))
            {
                using var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME);

                var subscriptions = _eventStore.GetHandlersForEvent(eventName);

                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicEventHandler;
                        if(handler == null) continue;
                        dynamic eventData = JObject.Parse(message);

                        await Task.Yield();
                        await handler.Handle(eventData);
                    }
                    else
                    {
                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        if(handler == null) continue;

                        var eventType = _eventStore.GetEventTypeByName(eventName);

                        var eventData = JsonConvert.DeserializeObject(message, eventType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                        await Task.Yield();
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { eventData });
                    }
                }
            }
            else
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            }
        }

        private IModel CreateConsumerChannel()
        {
            if (!_rabbitMQConnection.IsConnected)
                _rabbitMQConnection.TryConnect();
            var channel = _rabbitMQConnection.CreateModel();

            channel.ExchangeDeclare(exchange:BROKER_NAME, 
                                    type:ExchangeType.Direct);

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };
            return channel;
        }

        #endregion

    }
}
