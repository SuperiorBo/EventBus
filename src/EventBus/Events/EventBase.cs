using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBus.Events
{
    public class EventBase:IEventBase
    {
        [JsonConstructor]
        public EventBase(string eventId, DateTimeOffset eventAt)
        {
            EventId = eventId;
            EventAt = eventAt;
        }

        public EventBase(string eventId)
        {
            EventId = eventId;
            EventAt = DateTimeOffset.UtcNow;
        }

        public EventBase()
        {
            EventId = Guid.NewGuid().ToString("N");
            EventAt = DateTimeOffset.UtcNow;
        }

        [JsonProperty]
        public string EventId { get; private set; }

        [JsonProperty]
        public DateTimeOffset EventAt { get; private set; }
        
    }
}
