using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using EventBus.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventBus.Kafka
{
    public class DefaultKafkaConsumerConnection<TKey,TValue> : DefaultKafkaConnection
    {
        private readonly object sync_consumer = new object();
        private readonly ILogger<DefaultKafkaConsumerConnection<TKey,TValue>> _logger;
        private readonly ConsumerBuilder<TKey, TValue> _builder;
        private bool _disposed;
        private IConsumer<TKey, TValue> _connection;

        public event EventHandler<ConsumeResult<TKey, TValue>> OnMessageReceived;
        public event EventHandler<OperationCanceledException> CallCanceledException;

        public DefaultKafkaConsumerConnection(
            IEnumerable<KeyValuePair<string, string>> consumerConfig,
            ILogger<DefaultKafkaConsumerConnection<TKey, TValue>> logger,
            int retryCount = 5
            ):base(logger,consumerConfig,retryCount)
        {
            _logger = logger??new NullLogger<DefaultKafkaConsumerConnection<TKey, TValue>>();
            _builder = new ConsumerBuilder<TKey, TValue>(_config);
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

        public async Task Consume()
        {
            try
            {
                await Task.Run(() =>
                {
                    lock (sync_consumer)
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = _connection.Consume();

                                if (consumeResult.IsPartitionEOF)
                                {
                                    _logger.LogInformation(
                                        $"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");

                                    continue;
                                }

                                RaiseOnMessageReceived(consumeResult);
                            }
                            catch (ConsumeException e)
                            {
                                _logger.LogError($"Consume error: {e.Error.Reason}");
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException e)
            {
                RaiseCallCanceledException(e);
            }
        }

        private void RaiseCallCanceledException(OperationCanceledException exception)
        {
            var handler = CallCanceledException;

            if (handler == null)
            {
                _connection.Close();
            }
            else
            {
                handler.Invoke(this, exception);
            }
        }

        private void RaiseOnMessageReceived(ConsumeResult<TKey, TValue> consumeResult)
        {
            var handler = OnMessageReceived;
            handler?.Invoke(this, consumeResult);
        }
    }
}
