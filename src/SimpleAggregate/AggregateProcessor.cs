namespace SimpleAggregate
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class AggregateProcessor
    {
        private readonly IEventSource _eventSource;

        public AggregateProcessor(IEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public async Task ProcessAsync<T>(T aggregate, Action<T> aggregateAction) where T : IAggregate
        {
            await LoadAsync(aggregate);
            aggregateAction?.Invoke(aggregate);
            await SaveAsync(aggregate);
        }

        private async Task LoadAsync<T>(T aggregate) where T : IAggregate
        {
            var events = await _eventSource.LoadEventsAsync(aggregate.AggregateId);
            if (events != null)
            {
                aggregate.Rehydrate(events);
            }
        }

        private async Task SaveAsync<T>(T aggregate) where T : IAggregate
        {
            if (aggregate.UncommittedEvents.Any())
            {
                await _eventSource.SaveEventsAsync(aggregate.AggregateId, aggregate.UncommittedEvents, aggregate.CommittedEvents.Count);
            }
        }
    }
}
