﻿namespace SimpleAggregate
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class Aggregate
    {
        public ReadOnlyCollection<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();
        public ReadOnlyCollection<object> CommittedEvents => _committedEvents.AsReadOnly();
        public string AggregateId { get; protected set; }
        public object ConcurrencyKey { get; set; }  
        public bool ForbidUnregisteredEvents { get; protected set; } = false;
        private readonly List<object> _uncommittedEvents = new List<object>();
        private List<dynamic> _committedEvents = new List<dynamic>();

        protected void Apply<TEvent>(TEvent @event)
        {
            _uncommittedEvents.Add(@event);
            ApplyInternal(@event);
        }

        private void ApplyInternal<TEvent>(TEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event), "The event to be applied is null");

            if (this is IHandle<TEvent> aggregate)
            {
                aggregate.Handle(@event);
                return;
            } 
            
            if (ForbidUnregisteredEvents) 
                throw new UnregisteredEventException($"The requested event '{@event.GetType().FullName}' is not registered in '{GetType().FullName}' and could not be applied");
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