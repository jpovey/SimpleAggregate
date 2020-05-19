namespace SimpleAggregate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class AggregateProcessor<TAggregate> where TAggregate : Aggregate, new()
    {
        private readonly IAggregateRepository<TAggregate> _aggregateRepository;

        public AggregateProcessor(IAggregateRepository<TAggregate> aggregateRepository)
        {
            _aggregateRepository = aggregateRepository;
        }

        public async Task ProcessAsync(string aggregateId, Action<TAggregate> command, CancellationToken cancellationToken = default)
        {
            var aggregate = await _aggregateRepository.GetAsync(aggregateId, cancellationToken);
            command?.Invoke(aggregate);
            await _aggregateRepository.SaveAsync(aggregate, cancellationToken);
        }
    }
}