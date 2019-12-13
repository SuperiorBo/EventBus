using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly ProducerBuilder<Ignore, string> _builder;
        private bool _disposed;
        private IProducer<Ignore, string> _connection;

        public DefaultKafkaProducerConnection(
            IEnumerable<KeyValuePair<string, string>> producerConfig,
            ILogger<DefaultKafkaProducerConnection> logger,
            int retryCount = 5
        ) : base(logger, producerConfig, retryCount)
        {
            _logger = logger ?? new NullLogger<DefaultKafkaProducerConnection>();
            _builder = new ProducerBuilder<Ignore, string>(_config);
        }

        public override bool IsConnected => _connection != null && !_disposed;

        public override IProducer<Ignore,byte[]> CreateConnect()
        {
            return _connection;
        }

        public override Action Connection(IEnumerable<KeyValuePair<string, string>> config)
        {
            return () =>
            {
                _connection = _builder.Build();
            };
        }

        public override void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }
    }
}
