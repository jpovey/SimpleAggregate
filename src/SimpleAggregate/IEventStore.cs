namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventStore
    {
        Task<IEnumerable<object>> LoadAsync(string aggregateId);
        Task SaveAsync(string aggregateId, IEnumerable<object> events);
    }
}