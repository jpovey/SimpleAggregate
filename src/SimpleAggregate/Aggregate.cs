namespace SimpleAggregate
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class Aggregate : IAggregate
    {
        public ReadOnlyCollection<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();
        public ReadOnlyCollection<object> CommittedEvents => _committedEvents.AsReadOnly();
        public string AggregateId { get; }
        public bool IgnoreUnregisteredEvents { get; protected set; }
        private readonly List<object> _uncommittedEvents = new List<object>();
        private List<dynamic> _committedEvents = new List<dynamic>();

        protected Aggregate(string aggregateId)
        {
            AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId), "AggregateId must not be null");
        }

        protected void Apply<TEvent>(TEvent @event)
        {
            _uncommittedEvents.Add(@event);
            ApplyInternal(@event);
        }

        private void ApplyInternal<TEvent>(TEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event), "The event to be applied is null");

            var handler = this as IHandle<TEvent>;

            if (!IgnoreUnregisteredEvents && handler == null)
                throw new UnregisteredEventException($"The requested event '{@event.GetType().FullName}' is not registered in '{GetType().FullName}'");

            handler?.Handle(@event);
            
        }

        public void Rehydrate(IEnumerable<dynamic> events)
        {
             _committedEvents = events.ToList();
            foreach (var @event in _committedEvents) ApplyInternal(@event);
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }
    }
}
