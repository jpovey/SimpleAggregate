namespace SimpleAggregate.UnitTests.Domain
{
    using System.Threading.Tasks;
    using Events;
    
    public class BankAccount : Aggregate,
        IHandle<AccountCredited>,
        IHandle<AccountDebited>
    {
        public decimal Balance { get; private set; }

        public BankAccount(string aggregateId) : base(aggregateId)
        {
        }

        public BankAccount(string aggregateId, bool ignoreUnregisteredEvents) : base(aggregateId)
        {
            IgnoreUnregisteredEvents = ignoreUnregisteredEvents;
        }

        public async Task CreditAccount(decimal amount)
        {
            await CallDummyApi();
            this.Apply(new AccountCredited { Amount = amount });
        }

        public void DoNothing()
        {

        }

        private static Task CallDummyApi()
        {
            return Task.CompletedTask;
        }

        public void ApplyNullEvent()
        {
            this.Apply<AccountCredited>(null);
        }

        void IHandle<AccountCredited>.Handle(AccountCredited priceUpdated)
        {
            Balance += priceUpdated.Amount;
        }

        void IHandle<AccountDebited>.Handle(AccountDebited accountDebited)
        {
            Balance -= accountDebited.Amount;
        }
    }
}
