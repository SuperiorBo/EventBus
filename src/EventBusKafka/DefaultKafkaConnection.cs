using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;

namespace EventBusKafka
{
    public class DefaultKafkaConnection:IKafkaConnection
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool IsConnected { get; }
        public bool TryConnect()
        {
            throw new NotImplementedException();
        }

        public IProducer<TKey, TValue> CreateProducer<TKey, TValue>()
        {
            throw new NotImplementedException();
        }

        public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>()
        {
            throw new NotImplementedException();
        }
    }
}
