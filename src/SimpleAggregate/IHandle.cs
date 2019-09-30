namespace SimpleAggregate
{
    public interface IHandle<in TEvent>
    {
        void Handle(TEvent @event);
    }
}
