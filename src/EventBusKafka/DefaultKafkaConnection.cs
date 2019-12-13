using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using EventBus.Events;
using Microsoft.Extensions.Logging;
using Polly;

namespace EventBus.Kafka
{
    public abstract class DefaultKafkaConnection : IKafkaConnection
    {
        private ILogger<DefaultKafkaConnection> _logger;
        protected IEnumerable<KeyValuePair<string, string>> _config;
        private int _retryCount;

        object sync_root = new object();

        public DefaultKafkaConnection(
            ILogger<DefaultKafkaConnection> logger,
            IEnumerable<KeyValuePair<string, string>> config,
            int retryCount = 5
            )
        {
            _logger = logger;
            _config = config;
            _retryCount = retryCount;
        }

        public abstract bool IsConnected { get; }

        public abstract IClient CreateConnect();

        public bool TryConnect()
        {
            _logger.LogInformation("Kafka Client is trying to connect");

            lock (sync_root)
            {
                var policy = Policy.Handle<KafkaException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                        {
                            _logger.LogWarning(ex, "Kafka Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                        }
                    );

                policy.Execute(Connection);
            }

            if (IsConnected)
            {
                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: Kafka connections could not be created and opened");

                return false;
            }
        }

        public abstract void Connection();

        public abstract void Dispose();
    }
}
