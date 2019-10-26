namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEventSource
    {
        Task<EventSourceResult> LoadEventsAsync(string aggregateId, CancellationToken cancellationToken = default);
        Task SaveEventsAsync(string aggregateId, IEnumerable<object> events, object concurrencyKey, CancellationToken cancellationToken = default);
    }
}