namespace SimpleAggregate
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class AggregateRepository<TAggregate> : IAggregateRepository<TAggregate> where TAggregate : Aggregate, new()
    {
        private readonly IEventStream _eventStream;

        public AggregateRepository(IEventStream eventStream)
        {
            _eventStream = eventStream;
        }

        public async Task<TAggregate> GetAsync(string aggregateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId)) throw new ArgumentException("AggregateId must not be null or empty");

            var aggregate = new TAggregate();
            var eventStreamContext = await _eventStream.ReadAsync(aggregateId, cancellationToken);
            if (eventStreamContext.Events != null)
            {
                aggregate.Rehydrate(eventStreamContext.Events);
            }

            aggregate.ConcurrencyKey = eventStreamContext.ConcurrencyKey;

            return aggregate;
        }

        public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregate.AggregateId)) throw new ArgumentException("AggregateId must not be null or empty");

            if (aggregate.UncommittedEvents.Any())
            {
                await _eventStream.AppendAsync(aggregate.AggregateId, aggregate.UncommittedEvents, aggregate.ConcurrencyKey, cancellationToken);
            }
        }
    }
}