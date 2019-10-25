namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventSource
    {
        Task<EventSourceResult> LoadEventsAsync(string aggregateId);
        Task SaveEventsAsync(string aggregateId, IEnumerable<object> events, object concurrencyKey);
    }
}