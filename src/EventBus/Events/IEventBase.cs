using System;
using System.Collections.Generic;
using System.Text;

namespace EventBus.Events
{
    public interface IEventBase
    {
        string EventId { get; }

        DateTimeOffset EventAt { get; }
    }
}
