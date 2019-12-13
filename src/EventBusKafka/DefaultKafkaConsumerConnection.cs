using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Confluent.Kafka;
using EventBus.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventBus.Kafka
{
    public class DefaultKafkaConsumerConnection : DefaultKafkaConnection
    {
        private readonly ILogger<DefaultKafkaConsumerConnection> _logger;
        private readonly ConsumerBuilder<Ignore, string> _builder;
        private bool _disposed;
        private IConsumer<Ignore, string> _connection;


        public DefaultKafkaConsumerConnection(
            IEnumerable<KeyValuePair<string, string>> consumerConfig,
            ILogger<DefaultKafkaConsumerConnection> logger,
            int retryCount = 5
            ):base(logger,consumerConfig,retryCount)
        {
            _logger = logger??new NullLogger<DefaultKafkaConsumerConnection>();
            _builder = new ConsumerBuilder<Ignore, string>(_config);
        }

        public override bool IsConnected => _connection != null && !_disposed;

        public override IClient CreateConnect()
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
            if(_disposed) return;

            _disposed = true;

            try
            {
                _connection.Close();
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }
    }
}
