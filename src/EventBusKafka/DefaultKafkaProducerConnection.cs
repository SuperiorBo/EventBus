using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using EventBus.Events;
using EventBus.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventBus.Kafka
{
    public class DefaultKafkaProducerConnection : DefaultKafkaConnection
    {
        private readonly ILogger<DefaultKafkaProducerConnection> _logger;
        private readonly bool _disposed;
        private readonly IProducer<Ignore, string> _producer;

        public DefaultKafkaProducerConnection(
            IEnumerable<KeyValuePair<string, string>> consumerConfig,
            ILogger<DefaultKafkaProducerConnection> logger,
            int retryCount = 5
        ) : base(logger, consumerConfig, retryCount)
        {
            _logger = logger ?? new NullLogger<DefaultKafkaProducerConnection>();
            _builder = new ProducerBuilder<Ignore, string>(consumerConfig)
        }




        public override bool IsConnected => _producer != null && !_disposed;

        public override IClient CreateModel()
        {
            throw new NotImplementedException();
        }

        public override Action Connection(IEnumerable<KeyValuePair<string, string>> config)
        {
            return () =>
            {
                _builder = .Build();
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
