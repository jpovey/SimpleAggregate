namespace SimpleAggregate
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventRepository
    {
        Task<IEnumerable<object>> LoadEvents(string aggregateId);
        Task SaveEvents(string aggregateId, IEnumerable<object> events);
    }
}
