[![Build Status](https://jonpovey.visualstudio.com/SimpleAggregate/_apis/build/status/jpovey.SimpleAggregate?branchName=master)](https://jonpovey.visualstudio.com/SimpleAggregate/_build/latest?definitionId=1&branchName=master)

# SimpleAggregate
A package to help simplify applying events to a DDD aggregate.

# Aggregate
1. Create an aggregate which inherits from `Aggregate`
2. Set aggregate base properties
3. Register event handlers
4. Implement event handlers

```c#
public class BankAccount : Aggregate, //Step 1
    IHandle<AccountCredited> //Step 3
{
    public string AccountReference => AggregateId;
    public decimal Balance { get; private set; }

    // Step 2
    public BankAccount(string accountReference, bool ignoreUnregisteredEvents = false) 
        : base(accountReference, ignoreUnregisteredEvents)
    {
    }

    public async Task CreditAccount(decimal amount)
    {
        this.Apply(new AccountCredited { Amount = amount });
    }

    // Step 4
    void IHandle<AccountCredited>.Handle(AccountCredited priceUpdated)
    {
        Balance += priceUpdated.Amount;
    }
}

```

5. Declare a new instance of the aggregate
```c#
// Step 5
var accountReference = "REF123";
var account = new BankAccount(accountReference);
```
6. Supply events to hyrdate the aggregate
```c#
// Step 6
var events = new List<object>
{
    new AccountCredited { Amount = 50 }
};

account.Rehydrate(events);
```

7. Or action a command
```c#
// Step 7
account.CreditAccount(100);
```

## Settings

- `IgnoreUnregisteredEvents`: Control if the aggregate must handle all events it tries to apply. 
    - If set to `true` unregistered events will be ignored. 
    - If set to `false` unregistered events will throw an exception.


# Aggregate Processor
The aggregate processor allows users to rehydrate, command an action to be performed and commit new events against an aggregate.

The `AggregateProcessor` requires an instance of `IEventStore` which should be used to wrap the event data access implementation.

This class could be used in a command handler to perform some business logic against an aggregate.

```c#
public class CreditAccountHandler
{
    private readonly AggregateProcessor _processor;

    public CreditAccountHandler(AggregateProcessor processor)
    {
        _processor = processor;
    }

    public Task Handle(DepositFundsCommand command)
    {
        var bankAccount = new BankAccount(command.AccountReference);
        return _processor.Process(bankAccount, 
            account => account.CreditAccount(command.Amount));
    }
}

```
