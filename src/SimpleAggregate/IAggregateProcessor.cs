namespace SimpleAggregate
{
    using System;
    using System.Threading.Tasks;

    public interface IAggregateProcessor
    {
        Task Process<T>(T aggregate, Action<T> action) where T : IAggregate;
    }
}