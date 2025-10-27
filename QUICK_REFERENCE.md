# Event Sourcing Quick Reference Guide

## 📋 Cheat Sheet

### Core Pattern Structure

```csharp
// 1. DEFINE EVENTS (Immutable, Past Tense)
public class MoneyDeposited : DomainEvent
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
}

// 2. CREATE AGGREGATE
public class BankAccount
{
    // State
    private decimal _balance;
    
    // Command
    public void Deposit(decimal amount)
    {
        // Validate
        if (amount <= 0) throw new Exception();
        
        // Create event
        var @event = new MoneyDeposited(Id, amount);
        
        // Apply event
        Apply(@event);
        
        // Track for persistence
        _uncommittedEvents.Add(@event);
    }
    
    // Apply event (updates state)
    private void Apply(MoneyDeposited e)
    {
        _balance += e.Amount;
    }
}

// 3. STORE EVENTS
await eventStore.SaveEventsAsync(accountId, events);

// 4. REBUILD FROM EVENTS
var events = await eventStore.GetEventsAsync(accountId);
var account = BankAccount.LoadFromHistory(events);
```

## 🔑 Key Principles

### 1. Events Are Facts
```
✅ AccountCreated
✅ MoneyDeposited
✅ OrderPlaced
❌ CreateAccount (command, not event)
❌ DepositMoney (command, not event)
```

### 2. Events Are Immutable
```csharp
// ✅ CORRECT - Properties are init-only
public class MoneyDeposited
{
    public decimal Amount { get; init; }
}

// ❌ WRONG - Properties are mutable
public class MoneyDeposited
{
    public decimal Amount { get; set; }
}
```

### 3. State from Events
```csharp
// ❌ WRONG - Setting state directly
public void Deposit(decimal amount)
{
    _balance += amount;  // Direct state change
}

// ✅ CORRECT - State changed via events
public void Deposit(decimal amount)
{
    var @event = new MoneyDeposited(Id, amount);
    Apply(@event);  // Event changes state
}
```

### 4. Validation Before Events
```csharp
public void Withdraw(decimal amount)
{
    // ✅ Validate BEFORE creating event
    if (amount > _balance) 
        throw new InvalidOperationException("Insufficient funds");
    
    // Create event only if validation passes
    var @event = new MoneyWithdrawn(Id, amount);
    Apply(@event);
}
```

## 🎯 Common Patterns

### Pattern 1: Command Handler
```csharp
public class DepositMoneyHandler
{
    private readonly IRepository _repository;
    
    public async Task Handle(DepositMoney command)
    {
        // Load aggregate (rebuilds from events)
        var account = await _repository.GetById(command.AccountId);
        
        // Execute business logic (creates events)
        account.Deposit(command.Amount);
        
        // Persist events
        await _repository.Save(account);
    }
}
```

### Pattern 2: Event Handler (for Projections)
```csharp
public class AccountSummaryProjection
{
    public void Handle(MoneyDeposited @event)
    {
        var summary = _readDb.GetSummary(@event.AccountId);
        summary.Balance += @event.Amount;
        summary.TransactionCount++;
        _readDb.Update(summary);
    }
}
```

### Pattern 3: Snapshot
```csharp
// For performance with many events
public class Snapshot
{
    public Guid AggregateId { get; set; }
    public int Version { get; set; }
    public BankAccountState State { get; set; }
}

// Load from snapshot
var snapshot = await _snapshotStore.GetLatest(accountId);
var account = new BankAccount(snapshot.State);
var newEvents = await _eventStore.GetEventsSince(accountId, snapshot.Version);
account.LoadFromHistory(newEvents);
```

## ⚡ Quick Decisions

### When to Create a New Event?
```
User action that changes state → New Event
Examples:
- Button clicked → Command → Event
- API call received → Command → Event
- Timer triggered → Command → Event
```

### When to Create a Projection?
```
Need fast queries → Create Projection
Examples:
- Dashboard displays
- Search functionality
- Reports
- List views
```

### When to Use Snapshots?
```
Aggregate has > 1000 events → Consider Snapshot
Replay is slow → Use Snapshot
```

## 🚀 Implementation Checklist

### Setting Up Event Sourcing

- [ ] **Step 1**: Define your domain events
```csharp
public class OrderPlaced : DomainEvent { ... }
```

- [ ] **Step 2**: Create aggregate with:
  - State properties
  - Command methods (public)
  - Apply methods (private)
  - LoadFromHistory method

- [ ] **Step 3**: Implement Event Store
  - SaveEvents method
  - GetEvents method
  - Handle concurrency

- [ ] **Step 4**: Create Repository
  - Load aggregate (replay events)
  - Save aggregate (persist uncommitted events)

- [ ] **Step 5**: Build Projections (if needed)
  - Listen to events
  - Update read models

- [ ] **Step 6**: Add Snapshots (optional)
  - For performance optimization

## 🎨 Code Templates

### Event Template
```csharp
public class [EventName] : DomainEvent
{
    public Guid AggregateId { get; init; }
    public [Type] [Property] { get; init; }
    
    public [EventName](Guid aggregateId, [Type] property)
    {
        AggregateId = aggregateId;
        [Property] = property;
    }
}
```

### Aggregate Template
```csharp
public class [AggregateName]
{
    // State
    public Guid Id { get; private set; }
    private [Type] _state;
    
    private List<DomainEvent> _uncommittedEvents = new();
    
    // Command
    public void [CommandName]([Parameters])
    {
        // 1. Validate
        if (![ValidationRule]) throw new Exception();
        
        // 2. Create event
        var @event = new [EventName]([Parameters]);
        
        // 3. Apply event
        ApplyEvent(@event);
    }
    
    // Apply new event
    private void ApplyEvent(DomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
    
    // Update state
    private void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case [EventName] e:
                // Update state
                break;
        }
    }
    
    // Rebuild from events
    public static [AggregateName] LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        var aggregate = new [AggregateName]();
        foreach (var @event in events)
        {
            aggregate.Apply(@event);
        }
        return aggregate;
    }
}
```

## 🐛 Common Mistakes

### ❌ Mistake 1: Mutable Events
```csharp
// WRONG
public class MoneyDeposited
{
    public decimal Amount { get; set; }  // Mutable!
}
```

### ❌ Mistake 2: Changing State Directly
```csharp
// WRONG
public void Deposit(decimal amount)
{
    _balance += amount;  // Direct state change
    // No event created!
}
```

### ❌ Mistake 3: Validation After Event
```csharp
// WRONG
public void Withdraw(decimal amount)
{
    var @event = new MoneyWithdrawn(Id, amount);
    Apply(@event);
    
    if (_balance < 0)  // Too late!
        throw new Exception();
}
```

### ❌ Mistake 4: Complex Event Logic
```csharp
// WRONG - Events should be simple
public class ComplexBusinessRuleExecuted : DomainEvent
{
    // Don't put business logic in events
    public void ExecuteBusinessRule() { ... }
}
```

### ❌ Mistake 5: Updating Events
```csharp
// WRONG - Events are immutable
var @event = await _eventStore.GetEvent(eventId);
@event.Amount = 200;  // Never update events!
await _eventStore.SaveEvent(@event);
```

## 📊 Performance Tips

### 1. Use Snapshots
```csharp
// Save snapshot every 100 events
if (account.Version % 100 == 0)
{
    await _snapshotStore.Save(account.TakeSnapshot());
}
```

### 2. Cache Projections
```csharp
// Cache read models
var summary = _cache.Get(accountId) 
    ?? await _projectionStore.GetSummary(accountId);
```

### 3. Async Event Handlers
```csharp
// Update projections asynchronously
_eventBus.Publish(@event);  // Fire and forget
```

### 4. Batch Event Loading
```csharp
// Load events in batches
const int batchSize = 1000;
var events = await _eventStore.GetEvents(accountId, batchSize);
```

## 🔍 Testing Tips

### Test Events
```csharp
[Test]
public void Deposit_ShouldCreateMoneyDepositedEvent()
{
    var account = new BankAccount(id, "John", 1000m);
    
    account.Deposit(500m, "Salary");
    
    Assert.That(account.UncommittedEvents.Count, Is.EqualTo(2));
    Assert.That(account.UncommittedEvents.Last(), Is.TypeOf<MoneyDeposited>());
}
```

### Test State
```csharp
[Test]
public void Deposit_ShouldIncreaseBalance()
{
    var account = new BankAccount(id, "John", 1000m);
    
    account.Deposit(500m, "Salary");
    
    Assert.That(account.Balance, Is.EqualTo(1500m));
}
```

### Test Event Replay
```csharp
[Test]
public void LoadFromHistory_ShouldRebuildCorrectState()
{
    var events = new List<DomainEvent>
    {
        new AccountCreated(id, "John", 1000m),
        new MoneyDeposited(id, 500m, "Salary")
    };
    
    var account = BankAccount.LoadFromHistory(events);
    
    Assert.That(account.Balance, Is.EqualTo(1500m));
}
```

## 📚 Further Reading

- Martin Fowler: Event Sourcing
- Greg Young: CQRS and Event Sourcing
- Vernon, Vaughn: Implementing Domain-Driven Design
- Microsoft: CQRS Journey

---

**Remember**: Event Sourcing is powerful but adds complexity. Use it when you need its benefits!
