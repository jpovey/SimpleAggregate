namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public interface IAggregate
    {
        string AggregateId { get; }
        ReadOnlyCollection<object> UncommittedEvents { get; }
        ReadOnlyCollection<object> CommittedEvents { get; }
        void Rehydrate(IEnumerable<dynamic> events);
    }
}
