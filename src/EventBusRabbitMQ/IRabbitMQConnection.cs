using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace EventBus.RabbitMQ
{
    public interface IRabbitMQConnection 
        : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
