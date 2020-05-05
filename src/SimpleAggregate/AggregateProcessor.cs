namespace SimpleAggregate
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class AggregateProcessor
    {
        private readonly IEventSource _eventSource;

        public AggregateProcessor(IEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public async Task ProcessAsync<T>(T aggregate, Action<T> aggregateAction, CancellationToken cancellationToken = default) where T : IAggregate
        {
            var concurrencyKey = await ReadAsync(aggregate, cancellationToken);
            aggregateAction?.Invoke(aggregate);
            await CommitAsync(aggregate, concurrencyKey, cancellationToken);
        }

        private async Task<object> ReadAsync<T>(T aggregate, CancellationToken cancellationToken = default) where T : IAggregate
        {
            var eventSourceResult = await _eventSource.ReadEventsAsync(aggregate.AggregateId, cancellationToken);
            if (eventSourceResult.Events != null)
            {
                aggregate.Rehydrate(eventSourceResult.Events);
            }

            return eventSourceResult.ConcurrencyKey;
        }

        private async Task CommitAsync<T>(T aggregate, object concurrencyKey, CancellationToken cancellationToken = default) where T : IAggregate
        {
            if (aggregate.UncommittedEvents.Any())
            {
                await _eventSource.CommitEventsAsync(aggregate.AggregateId, aggregate.UncommittedEvents, concurrencyKey, cancellationToken);
            }
        }
    }
}
