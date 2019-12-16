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
    public class DefaultKafkaProducerConnection<TKey, TValue> : DefaultKafkaConnection
    {
        private readonly ILogger<DefaultKafkaProducerConnection<TKey, TValue>> _logger;
        private readonly ProducerBuilder<TKey, TValue> _builder;
        private bool _disposed;
        private IProducer<TKey, TValue> _connection;

        public DefaultKafkaProducerConnection(
            IEnumerable<KeyValuePair<string, string>> producerConfig,
            ILogger<DefaultKafkaProducerConnection<TKey, TValue>> logger,
            int retryCount = 5
        ) : base(logger, producerConfig, retryCount)
        {
            _logger = logger ?? new NullLogger<DefaultKafkaProducerConnection<TKey, TValue>>();
            _builder = new ProducerBuilder<TKey, TValue>(_config);
        }

        public override bool IsConnected => _connection != null && !_disposed;

        public override IClient CreateConnect()
        {
            return _connection;
        }

        public override Action Connection()
        {
            return () =>
            {
                _connection = _builder
                    .SetErrorHandler((client, error) =>
                    {
                        _logger.LogCritical(error.Reason);
                    })
                    .Build();
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
