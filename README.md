[![Build Status](https://jonpovey.visualstudio.com/SimpleAggregate/_apis/build/status/Publish?branchName=master)](https://jonpovey.visualstudio.com/SimpleAggregate/_build/latest?definitionId=16&branchName=master)

# SimpleAggregate
A package to help simplify applying events to a DDD aggregate.

## How to use

### Aggregate
Aggregates are built by creating an instance which inherits from the Aggregate base class. Once created aggregates can be rehydrated by appling events from history or updated by applying new events.

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
    public BankAccount(string accountReference) : base(accountReference)
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

### Aggregate Settings

**Ignore Unregistered Events**

By default the aggregate will ignore events which have not been registered. But, based on the architecture of your system some services may require all events to be handled. To ensure all events are registered set `IgnoreUnregisteredEvents` to `false` when setting up the aggregate, this will cause an exception to be thrown when handing an unexpected event.

- If set to `true` or default unregistered events will be ignored. 
- If set to `false` unregistered events will throw an `UnregisteredEventException`.

```c#
public BankAccount(string bankAccountReference) : base (bankAccountReference)
{
    IgnoreUnregisteredEvents = false;
}
```

### Aggregate Processor
The aggregate processor allows users to rehydrate, command an action to be performed and commit new events against an aggregate.

The `AggregateProcessor` requires an instance of `IEventSource` which should be used to wrap the data access implementation of your desired event source.

This class could be used in a command handler to perform some business logic against an aggregate.

```c#
public class CreditAccountHandler
{
    private readonly AggregateProcessor _processor;

    public CreditAccountHandler(AggregateProcessor processor)
    {
        _processor = processor;
    }

    public Task Handle(CreditAccountCommand command)
    {
        var bankAccount = new BankAccount(command.AccountReference);
        return _processor.ProcessAsync(bankAccount, 
            account => account.CreditAccount(command.Amount));
    }
}

```

### Event Source
The `IEventSource` interfaces provides an abstraction to allow the `AggregateProcessor` to integrate with multiple types of database. When implementing your own processor a concrete IEventSource implementation should be used to wrap the data access layer of your event source/store.

**Concurrency**

To maintain a consistent state in the event store the `AggregateProcessor` uses a concurrency key when saving uncommitted events. This value must be returned from the `LoadEventsAsync` as part of the `EventSourceResult` which is turn used by `SaveEventsAsync`. Different database implementations use different types of concurrency key so use one which matches your database schema.

## Build and publish
#### Pipelines
Build pipelines are managed in Azure Devops
- [Build and Test](https://jonpovey.visualstudio.com/SimpleAggregate/_build?definitionId=17)
- [Publish](https://jonpovey.visualstudio.com/SimpleAggregate/_build?definitionId=16)

#### Nuget package
https://www.nuget.org/packages/SimpleAggregate/
