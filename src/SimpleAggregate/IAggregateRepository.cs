namespace SimpleAggregate
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IAggregateRepository<TAggregate>
    {
        Task<TAggregate> GetAsync(string aggregateId, CancellationToken cancellationToken = default);
        Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    }
}