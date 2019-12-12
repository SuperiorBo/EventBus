using System;
using System.Collections.Generic;
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

        public DefaultKafkaConsumerConnection(
            IEnumerable<KeyValuePair<string, string>> consumerConfig,
            ILogger<DefaultKafkaConsumerConnection> logger,
            int retryCount = 5
            ):base(logger,consumerConfig,retryCount)
        {
            _logger = logger??new NullLogger<DefaultKafkaConsumerConnection>();
        }

        public override bool IsConnected { get; }
        public override IClient CreateModel()
        {
            throw new NotImplementedException();
        }

        public override Action Connection(IEnumerable<KeyValuePair<string, string>> config)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
