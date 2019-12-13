using System;
using Confluent.Kafka;
using EventBus.Events;

namespace EventBus.Kafka
{
    public interface IKafkaConnection : IDisposable
    {

        bool IsConnected { get; }

        bool TryConnect();

        IClient CreateConnect();
    }
}
