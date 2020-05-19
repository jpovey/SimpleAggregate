namespace SimpleAggregate.UnitTests.Domain
{
    using Events;

    public class BankAccount : Aggregate,
        IHandle<AccountCreated>,
        IHandle<AccountCredited>,
        IHandle<AccountDebited>
    {
        public decimal Balance { get; private set; }

        public BankAccount()
        {
            ForbidUnregisteredEvents = false;
        }

        public BankAccount(string accountId)
        {
            this.Apply(new AccountCreated { AccountId = accountId});
        }

        public BankAccount(bool forbidUnregisteredEvents)
        {
            ForbidUnregisteredEvents = forbidUnregisteredEvents;
        }

        public void CreditAccount(decimal amount)
        {
            this.Apply(new AccountCredited { Amount = amount });
        }

        public void DebitAccount(decimal amount)
        {
            this.Apply(new AccountDebited { Amount = amount });
        }

        public void ApplyNullEvent()
        {
            this.Apply<AccountCredited>(null);
        }

        public void Handle(AccountCreated accountCreated)
        {
            AggregateId = accountCreated.AccountId;
        }

        public void Handle(AccountCredited accountCredited)
        {
            Balance += accountCredited.Amount;
        }

        public void Handle(AccountDebited accountDebited)
        {
            Balance -= accountDebited.Amount;
        }
    }
}
