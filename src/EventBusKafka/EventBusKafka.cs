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
        private readonly IKafkaConnection _kafkaConnection;
        private readonly IEventStore _eventStore;
        private readonly ILogger<EventBusKafka> _logger;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "default_event_bus";
        private readonly int _retryCount;


        private string _topicName;
        private IConsumer<string, IEventBase> _consumer;

        public EventBusKafka(
            IKafkaConnection kafkaConnection,
            IEventStore eventStore,
            ILogger<EventBusKafka> logger,
            ILifetimeScope autofac,
            string topicName = null,
            int retryCount = 5
        )
        {
            _kafkaConnection = kafkaConnection;
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

            if (!_kafkaConnection.IsConnected)
                _kafkaConnection.TryConnect();

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

        private IConsumer<string, IEventBase> CreateConsumer()
        {
            var consumer = new ConsumerBuilder<string, IEventBase>(consumerConfig).Build();

            return consumer;
        }


        private void EventStore_OnEventRemoved(object sender, string e)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
