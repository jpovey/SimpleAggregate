namespace SimpleAggregate
{
    using System.Collections.Generic;

    public class EventSourceResult
    {
        public readonly IEnumerable<object> Events;
        public readonly object ConcurrencyKey;

        public EventSourceResult(IEnumerable<object> events, object concurrencyKey)
        {
            Events = events;
            ConcurrencyKey = concurrencyKey;
        }
    }
}