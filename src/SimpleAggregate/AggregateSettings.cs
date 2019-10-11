namespace SimpleAggregate
{
    public sealed class AggregateSettings
    {
        static AggregateSettings()
        {
        }

        public bool IgnoreUnregisteredEvents { get; set; }
    }
}