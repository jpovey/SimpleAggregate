[![Build Status](https://jonpovey.visualstudio.com/SimpleAggregate/_apis/build/status/Publish?branchName=master)](https://jonpovey.visualstudio.com/SimpleAggregate/_build/latest?definitionId=16&branchName=master)


# SimpleAggregate
A package to help simplify applying events to a DDD aggregate. 

## Features

- A simple `Aggregate` base class used to rehydrate and apply events to an aggregate
- An `AggregateProcessor` to manage aggregates in combination with an event source

## How to use

```
Install-Package SimpleAggregate
```

### Aggregate

Create an aggregate by defining an instance which inherits from the `Aggregate` base class. Once created, aggregates can be rehydrated by applying events from history or updated by applying new events.

1. Define an aggregate which inherits from `Aggregate`
2. Configure constructor to set AggregateId base property
3. Register event handlers
4. Implement event handlers
5. Declare aggregate commands

```c#
public class BankAccount : Aggregate, //Step 1 
    IHandle<AccountCredited>, //Step 3
    IHandle<AccountDebited> 
{
    public decimal Balance { get; private set; }

    // Step 2
    public BankAccount(string aggregateId) : base(aggregateId)
    {
    }

    // Step 5
    public async Task CreditAccount(decimal amount)
    {
        this.Apply(new AccountCredited { Amount = amount });
    }

    public async Task DebitAccount(decimal amount)
    {
        this.Apply(new AccountDebited { Amount = amount });
    }

    // Step 4
    void IHandle<AccountCredited>.Handle(AccountCredited accountCredited)
    {
        Balance += accountCredited.Amount;
    }

    void IHandle<AccountDebited>.Handle(AccountDebited accountDebited)
    {
        Balance -= accountDebited.Amount;
    }
}

```

6. Declare a new instance of the aggregate
```c#
// Step 6
var accountReference = "REF123";
var account = new BankAccount(accountReference);
```

7. Hydrate the aggregate using existing events
```c#
// Step 7
var events = new List<object>
{
    new AccountCredited { Amount = 50 },
    new AccountDebited { Amount = 25 }
    new AccountCredited { Amount = 5 },
};

account.Rehydrate(events);
```

8. Or invoke commands
```c#
// Step 8
account.CreditAccount(100);
account.DebitAccount(15);
```

### Aggregate Settings

**Ignore Unregistered Events**

By default the aggregate will ignore events which have not been registered. But, based on the architecture of your system some services may require all events to be handled. 

To ensure all events are registered set `IgnoreUnregisteredEvents` to `false` when setting up the aggregate, this will cause an exception to be thrown when handing an unexpected event.

- If set to `true` or default unregistered events will be ignored. 
- If set to `false` unregistered events will throw an `UnregisteredEventException`.

```c#
public BankAccount(string bankAccountReference) : base (bankAccountReference)
{
    IgnoreUnregisteredEvents = false;
}
```

### Aggregate Processor
The aggregate processor can be used to orchestrate the flow of applying events to aggregates in combination with an event source.

The `AggregateProcessor` once invoked will:
- Read existing events from the event source
- Rehydrate the aggregate
- Command an action to be executed against the aggregate
- Commit new events to the event source

An example usage of the `AggregateProcessor` could be in a command handler to perform some business logic against an aggregate.

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
        //Declare aggregate
        var bankAccount = new BankAccount(command.AccountReference);

        //Process aggregate
        return _processor.ProcessAsync(bankAccount, 
            account => account.CreditAccount(command.Amount));
    }
}

```

### Event Source
The `IEventSource` interfaces provides an abstraction to allow the `AggregateProcessor` to integrate with multiple types of database.


 When implementing your own aggregate processor a concrete `IEventSource` implementation is required to wrap the data access layer of your desired event source/store.

**Concurrency**

To provide a consistent state in the event store a concurrency key reference is maintained by the `AggregateProcessor` after reading the event stream. This concurrency key is then used when committing events,

When implementing your own `IEventSource` this concurrency key should be used to maintain database concurrency in your own system. Different database implementations use different types of concurrency key so use one which matches your database schema.

## Build and publish
#### Pipelines
Build pipelines are managed in Azure Devops
- [Build and Test](https://jonpovey.visualstudio.com/SimpleAggregate/_build?definitionId=17)
- [Publish](https://jonpovey.visualstudio.com/SimpleAggregate/_build?definitionId=16)

#### Nuget package
https://www.nuget.org/packages/SimpleAggregate/
