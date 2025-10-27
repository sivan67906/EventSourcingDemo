# Event Sourcing in .NET - Complete Guide

## 🎯 What You'll Learn

This project demonstrates Event Sourcing pattern in .NET with a practical banking system example.

## 📚 Core Concepts Explained

### 1. **Events (Domain Events)**
Events are immutable facts about something that happened in the past.

**Key characteristics:**
- Named in past tense (e.g., `AccountCreated`, `MoneyDeposited`)
- Immutable - once created, they never change
- Contain all data needed to describe what happened
- Have a timestamp and version number

**Example:**
```csharp
public class MoneyDeposited : DomainEvent
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }
}
```

### 2. **Aggregate**
An aggregate is a domain object that uses events to track its state.

**Key responsibilities:**
- Validate business rules BEFORE creating events
- Apply events to update internal state
- Track uncommitted events (not yet saved)
- Provide methods for domain operations

**Example flow:**
```
User calls: account.Deposit(100, "Salary")
    ↓
Aggregate validates: amount > 0, account not closed
    ↓
Creates event: MoneyDeposited(accountId, 100, "Salary")
    ↓
Applies event: balance += 100
    ↓
Tracks event: uncommittedEvents.Add(event)
```

### 3. **Event Store**
Database that stores all events in chronological order.

**Key characteristics:**
- Append-only (no updates or deletes)
- Source of truth for the system
- Supports optimistic concurrency
- Provides audit trail

### 4. **Event Replay**
Process of rebuilding an aggregate's state by replaying all its events.

**How it works:**
```
Load events from Event Store
    ↓
Create empty aggregate
    ↓
Apply each event in order
    ↓
Result: Current state reconstructed
```

### 5. **Projections (Read Models)**
Optimized views built from events for querying.

**Benefits:**
- Fast queries (no event replay needed)
- Can have multiple projections from same events
- Can be rebuilt anytime from events

## 🏗️ Project Structure

```
EventSourcingDemo/
├── Domain/
│   ├── Events/
│   │   └── Events.cs              # Domain events
│   └── Aggregates/
│       └── BankAccount.cs         # Aggregate root
├── Infrastructure/
│   ├── EventStore.cs              # Event storage
│   └── BankAccountRepository.cs   # Repository
├── ReadModels/
│   └── AccountSummaryProjection.cs # Read model
└── Program.cs                      # Demo application
```

## 🚀 How to Run

### Prerequisites
- .NET 8.0 SDK or later

### Steps
1. Navigate to the project directory:
   ```bash
   cd EventSourcingDemo
   ```

2. Run the project:
   ```bash
   dotnet run
   ```

## 🔍 What the Demo Shows

### Scenario 1: Create Account
- Creates a new bank account with initial balance
- Shows how `AccountCreated` event is generated

### Scenario 2: Transactions
- Deposits and withdrawals
- Shows how multiple events are created and stored

### Scenario 3: Event Replay
- Loads account from scratch by replaying all events
- Demonstrates state reconstruction

### Scenario 4: Multiple Accounts
- Works with multiple aggregates simultaneously
- Each has its own event stream

### Scenario 5: Projections
- Builds a read model from all events
- Shows optimized view for queries

### Scenario 6: Audit Trail
- Displays complete event history
- Shows every change that occurred

### Scenario 7: Business Rules
- Demonstrates validation
- Shows how invalid operations are rejected

### Scenario 8: Close Account
- Closes an account
- Shows state transitions

## 💡 Key Benefits

### 1. Complete Audit Trail
Every change is recorded. You can see:
- What happened
- When it happened
- Who made the change
- Why it happened

### 2. Time Travel
Rebuild state at any point in time by replaying events up to that point.

### 3. Event Replay for Debugging
When bugs occur, replay events to understand exactly what happened.

### 4. Multiple Views
Create different projections from the same events:
- Summary view
- Transaction history view
- Analytics view
- etc.

### 5. Business Intelligence
Events are valuable data for:
- Analyzing user behavior
- Finding patterns
- Making business decisions

## 🎓 Advanced Concepts

### Snapshots
For aggregates with many events, you can create snapshots to improve performance:
```csharp
// Instead of replaying 10,000 events:
1. Load latest snapshot (state at event 9,000)
2. Replay only events 9,001-10,000
```

### CQRS (Command Query Responsibility Segregation)
Event Sourcing pairs well with CQRS:
- **Commands**: Change state (use aggregates)
- **Queries**: Read data (use projections)

### Event Versioning
As your system evolves, events may need to change:
```csharp
// Old version
public class MoneyDeposited { public decimal Amount; }

// New version with currency
public class MoneyDepositedV2 { 
    public decimal Amount; 
    public string Currency; 
}

// Use upcasters to convert old events
```

### Event Store Databases
For production, use specialized event stores:
- **EventStoreDB** (Dedicated event sourcing database)
- **SQL Server** (Using temporal tables)
- **PostgreSQL** (Using jsonb)
- **Azure Cosmos DB**

## 🔧 Real-World Use Cases

### 1. Financial Systems
- Banking transactions
- Payment processing
- Accounting systems

### 2. E-commerce
- Order processing
- Inventory management
- Shopping cart

### 3. Healthcare
- Patient records
- Treatment history
- Audit requirements

### 4. Collaboration Tools
- Document editing history
- Version control
- Change tracking

## ⚠️ Considerations

### When to Use Event Sourcing
✅ Need complete audit trail
✅ Complex business rules
✅ Temporal queries (state at specific time)
✅ Multiple views of same data
✅ High value of historical data

### When NOT to Use
❌ Simple CRUD applications
❌ No audit requirements
❌ Limited development resources
❌ Team unfamiliar with pattern

### Challenges
- **Complexity**: More complex than traditional approach
- **Event Schema**: Need to handle event versioning
- **Query Performance**: May need projections for fast queries
- **Learning Curve**: Team needs to understand the pattern

## 📖 Learn More

### Recommended Reading
- "Domain-Driven Design" by Eric Evans
- "Implementing Domain-Driven Design" by Vaughn Vernon
- "Patterns, Principles, and Practices of Domain-Driven Design" by Scott Millett

### Resources
- Martin Fowler's Event Sourcing: https://martinfowler.com/eaaDev/EventSourcing.html
- Greg Young's Event Store: https://eventstore.com
- Microsoft's CQRS Journey: https://learn.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10)

## 🤝 Next Steps

1. **Run the demo** - See event sourcing in action
2. **Modify the code** - Add new events or operations
3. **Try different scenarios** - Experiment with the pattern
4. **Build your own** - Apply to your domain

---

**Questions?** Review the code comments - each file is extensively documented!
