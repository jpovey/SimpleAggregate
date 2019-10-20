namespace SimpleAggregate
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class AggregateProcessor
    {
        private readonly IEventStore _eventStore;

        public AggregateProcessor(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task ProcessAsync<T>(T aggregate, Action<T> action) where T : IAggregate
        {
            await LoadAsync(aggregate);
            action?.Invoke(aggregate);
            await SaveAsync(aggregate);
        }

        private async Task LoadAsync<T>(T aggregate) where T : IAggregate
        {
            var events = await _eventStore.LoadAsync(aggregate.AggregateId);
            if (events != null)
            {
                aggregate.Rehydrate(events);
            }
        }

        private async Task SaveAsync<T>(T aggregate) where T : IAggregate
        {
            if (aggregate.UncommittedEvents.Any())
            {
                await _eventStore.SaveAsync(aggregate.AggregateId, aggregate.UncommittedEvents);
            }
        }
    }
}
