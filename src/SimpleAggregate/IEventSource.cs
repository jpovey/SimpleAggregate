namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEventSource
    {
        Task<EventSourceResult> ReadEventsAsync(string aggregateId, CancellationToken cancellationToken = default);
        Task CommitEventsAsync(string aggregateId, IEnumerable<object> events, object concurrencyKey, CancellationToken cancellationToken = default);
    }
}