[![Build Status](https://jonpovey.visualstudio.com/SimpleAggregate/_apis/build/status/Publish?branchName=master)](https://jonpovey.visualstudio.com/SimpleAggregate/_build/latest?definitionId=16&branchName=master)


# SimpleAggregate
A package to help simplify applying events to a DDD aggregate from an event stream. 

## Features

- A simple `Aggregate` base class used to rehydrate and apply events to an aggregate
- An `AggregateProcessor` to manage aggregates in combination with an event stream

# How to use

```
Install-Package SimpleAggregate
```

## Aggregate

Create an aggregate by defining an instance which inherits from the `Aggregate` base class. Once created, aggregates can be rehydrated by applying events from history or updated by applying new events.

1. Introduce an aggregate which inherits from the `Aggregate` base class
2. Register event handlers
3. Implement event handlers
4. Define aggregate commands

```c#
public class BankAccount : Aggregate, //Step 1 
    IHandle<AccountCreated>, //Step 2
    IHandle<AccountCredited>, 
    IHandle<AccountDebited> 
{
    public decimal Balance { get; private set; }
  
    // Step 4
    public async Task CreateAccount(string accountId)
    {
        this.Apply(new AccountCreated{ AccountId = accountId });
    }

    public async Task CreditAccount(decimal amount)
    {
        this.Apply(new AccountCredited { Amount = amount });
    }

    public async Task DebitAccount(decimal amount)
    {
        this.Apply(new AccountDebited { Amount = amount });
    }

    // Step 3
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

```

5. Create a new instance of the aggregate
```c#
// Step 5
var account = new BankAccount();
```

6. Hydrate the aggregate using existing events
```c#
// Step 6
var events = new List<object>
{
    new AccountCreated { AccountId = "Ref123" },
    new AccountCredited { Amount = 50 },
    new AccountDebited { Amount = 25 },
    new AccountCredited { Amount = 5 },
};

account.Rehydrate(events);
```

7. Or invoke commands to apply new events
```c#
// Step 7
account.CreditAccount(100);
account.DebitAccount(15);
```

### Aggregate Settings

**Forbid Unregistered Events**

By default the aggregate will ignore events which have not been registered. But, based on the architecture of your system some aggregates may require all events to be applied. 

To force all events to be handled set `ForbidUnregisteredEvents` to `true` when defining the aggregate, this will cause an exception to be thrown when handing an unexpected event.

- If set to `false` or default unregistered events will be ignored. 
- If set to `true` unregistered events will throw an `UnregisteredEventException`.

```c#
public BankAccount()
{
    ForbidUnregisteredEvents = true;
}
```

## Aggregate Processor
The aggregate processor should be used to orchestrate the flow of commanding an aggregate. 

The aggregate processor uses `IAggregateRepository` and in turn `IEventStream` to load and save existing aggregates.

The `AggregateProcessor` once invoked will:
- Use the aggregate repository to to read existing events from the event stream
- Use the aggregate repository to rehydrate the aggregate using loaded events
- Command an action to be executed against the aggregate
- Use the aggregate repository to append new events to the event stream

An example usage of the `AggregateProcessor` could be in a command handler to perform some business logic against an aggregate.

```c#
public class CreditAccountHandler
{
    private readonly AggregateProcessor _processor;

    public CreditAccountHandler(AggregateProcessor processor)
    {
        _processor = processor;
    }

    public Task Handle(CreditAccount command)
    {
        var amount = ConvertAmountToGbpBusinessLogic(command);
        return _processor.ProcessAsync(command.AccountReference, 
            x => x.CreditAccount(amount));
    }
}

```

### Aggregate Repository and Event Stream
The `AggregateProcessor` uses an `AggregateRepository` which requires an `IEventStream` to provide an abstraction between multiple types of databases which can be used to store events.

If using aggregate processor then a custom `IEventStream` concrete implementation is required to wrap the data access layer of your desired event store.

### Concurrency

To provide a consistent state in the event stream a reference to the `ConcurrencyKey` read from the `IEventStream` is stored against the aggregate when loaded from the aggregate repository. This concurrency key is then used when appending new events back to the stream.

When implementing your own `IEventStream` this concurrency key, returned as part of the `EventStreamContext`, should be used to maintain database concurrency in your own system. Different database implementations use different types of concurrency key so use one which matches your database schema.

### Dependency injection example setup
```c#
services.AddSingleton<AggregateProcessor<BankAccount>>();
services.AddSingleton<IAggregateRepository<BankAccount>, AggregateRepository<BankAccount>>();
services.AddSingleton<IEventStream, CustomEventStream>();
```

# Build and publish
### Pipelines
Build pipelines are managed in Azure Devops
- [Build and Test](https://jonpovey.visualstudio.com/SimpleAggregate/_build?definitionId=17)
- [Publish](https://jonpovey.visualstudio.com/SimpleAggregate/_build?definitionId=16)

### Nuget package
https://www.nuget.org/packages/SimpleAggregate/
