namespace SimpleAggregate
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class AggregateProcessor : IAggregateProcessor
    {
        private readonly IEventStore _eventStore;

        public AggregateProcessor(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task Process<T>(T aggregate, Action<T> action) where T : IAggregate
        {
            await Load(aggregate);
            action?.Invoke(aggregate);
            await Save(aggregate);
        }

        private async Task Load<T>(T aggregate) where T : IAggregate
        {
            var events = await _eventStore.LoadEvents(aggregate.AggregateId);
            if (events != null)
            {
                aggregate.Rehydrate(events);
            }
        }

        private async Task Save<T>(T aggregate) where T : IAggregate
        {
            if (aggregate.UncommittedEvents.Any())
            {
                await _eventStore.SaveEvents(aggregate.AggregateId, aggregate.UncommittedEvents);
            }
        }
    }
}
