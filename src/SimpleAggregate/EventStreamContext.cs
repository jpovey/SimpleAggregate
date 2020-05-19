namespace SimpleAggregate
{
    using System.Collections.Generic;

    public class EventStreamContext
    {
        public readonly IEnumerable<object> Events;
        public readonly object ConcurrencyKey;

        public EventStreamContext(IEnumerable<object> events, object concurrencyKey)
        {
            Events = events;
            ConcurrencyKey = concurrencyKey;
        }
    }
}