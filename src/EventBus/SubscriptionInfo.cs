using System;
using System.Collections.Generic;
using System.Text;

namespace EventBus
{
    public partial class EventStoreInMemory
    {

        public class SubscriptionInfo
        {
            public bool IsDynamic { get; }
            public Type HandlerType { get; }

            public SubscriptionInfo(bool isDynamic, Type handlerType)
            {
                IsDynamic = isDynamic;
                HandlerType = handlerType;
            }

            public static SubscriptionInfo Dynamic(Type handlerType) => new SubscriptionInfo(true,handlerType);

            public static SubscriptionInfo Typed(Type handlerType) => new SubscriptionInfo(false,handlerType);
        }
    }
}
