namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEventStream
    {
        Task<EventStreamContext> ReadAsync(string streamId, CancellationToken cancellationToken = default);
        Task AppendAsync(string streamId, IEnumerable<object> events, object concurrencyKey, CancellationToken cancellationToken = default);
    }
}