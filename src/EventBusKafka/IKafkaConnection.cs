using System;
using Confluent.Kafka;

namespace EventBusKafka
{
    public interface IKafkaConnection : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IProducer<TKey, TValue> CreateProducer<TKey,TValue>();

        IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>();
    }
}
