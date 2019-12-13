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
using Microsoft.Extensions.Logging;
using Polly;

namespace EventBus.Kafka
{
    public class EventBusKafka : IEventBus, IDisposable
    {
        private readonly DefaultKafkaProducerConnection _kafkaProducerConnection;
        private readonly DefaultKafkaConsumerConnection _kafkaConsumerConnection;
        private readonly IEventStore _eventStore;
        private readonly ILogger<EventBusKafka> _logger;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "default_event_bus";
        private readonly int _retryCount;


        private string _topicName;
        private IConsumer<Ignore, byte[]> _consumer;

        public EventBusKafka(
            DefaultKafkaProducerConnection kafkaProducerConnection,
            DefaultKafkaConsumerConnection kafkaConsumerConnection,
            IEventStore eventStore,
            ILogger<EventBusKafka> logger,
            ILifetimeScope autofac,
            string topicName = null,
            int retryCount = 5
        )
        {
            _kafkaProducerConnection = kafkaProducerConnection;
            _kafkaConsumerConnection = kafkaConsumerConnection;
            _eventStore = eventStore ?? new EventStoreInMemory();
            _logger = logger;
            _autofac = autofac;
            _topicName = topicName;
            _retryCount = retryCount;
            _consumer = CreateConsumer();
            _eventStore.OnEventRemoved += EventStore_OnEventRemoved;
        }

        public void Dispose()
        {
           _consumer?.Close();
           _eventStore.Clear();
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEventBase
        {
            if (!_eventStore.HasSubscriptionsForEvent<TEvent>())
                throw new ArgumentException("");

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

            using var producer = _kafkaProducerConnection.CreateConnect() as IProducer<Ignore, byte[]>;

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                _logger.LogTrace("Publishing event to Kafka: {EventId}", @event.EventId);

                producer.ProduceAsync(
                    _topicName,
                    new Message<Ignore, byte[]>
                    {
                        Value = body
                    });
            });
        }

        public void Subscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            throw new NotImplementedException();
        }

        public void SubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

        #region Private Method

        private IConsumer<Ignore,byte[]> CreateConsumer()
        {
            if (_kafkaConsumerConnection.IsConnected)
                _kafkaConsumerConnection.TryConnect();
            return _kafkaConsumerConnection.CreateConnect() as IConsumer<Ignore, byte[]>;
        }


        private void EventStore_OnEventRemoved(object sender, string e)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
