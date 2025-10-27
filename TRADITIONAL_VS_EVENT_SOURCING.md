# Traditional vs Event Sourcing - Side by Side Comparison

## 📊 Same Feature, Two Approaches

Let's implement a bank account feature using both approaches to see the differences.

## Traditional CRUD Approach

### Database Schema
```sql
CREATE TABLE Accounts (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AccountHolder NVARCHAR(100),
    Balance DECIMAL(18,2),
    IsClosed BIT,
    LastModified DATETIME
)

CREATE TABLE Transactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AccountId UNIQUEIDENTIFIER,
    Amount DECIMAL(18,2),
    Type NVARCHAR(20),  -- 'Deposit' or 'Withdrawal'
    Description NVARCHAR(200),
    TransactionDate DATETIME
)
```

### C# Code (Traditional)
```csharp
public class BankAccount
{
    public Guid Id { get; set; }
    public string AccountHolder { get; set; }
    public decimal Balance { get; set; }
    public bool IsClosed { get; set; }
    public DateTime LastModified { get; set; }
}

public class BankAccountService
{
    private readonly DbContext _db;
    
    public async Task Deposit(Guid accountId, decimal amount)
    {
        // Load account
        var account = await _db.Accounts.FindAsync(accountId);
        
        // Validate
        if (account.IsClosed)
            throw new Exception("Account is closed");
        
        // Update state directly
        account.Balance += amount;
        account.LastModified = DateTime.UtcNow;
        
        // Optional: Log transaction
        _db.Transactions.Add(new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            Type = "Deposit",
            TransactionDate = DateTime.UtcNow
        });
        
        // Save
        await _db.SaveChangesAsync();
    }
    
    public async Task Withdraw(Guid accountId, decimal amount)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        
        if (account.IsClosed)
            throw new Exception("Account is closed");
            
        if (account.Balance < amount)
            throw new Exception("Insufficient funds");
        
        // Update state directly
        account.Balance -= amount;
        account.LastModified = DateTime.UtcNow;
        
        _db.Transactions.Add(new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            Type = "Withdrawal",
            TransactionDate = DateTime.UtcNow
        });
        
        await _db.SaveChangesAsync();
    }
}
```

### What Gets Stored (Traditional)
```
Accounts Table:
┌──────────────────────────────────────┬──────────────┬─────────┐
│ Id                                   │ AccountHolder│ Balance │
├──────────────────────────────────────┼──────────────┼─────────┤
│ 123...                               │ John Doe     │ $1400   │
└──────────────────────────────────────┴──────────────┴─────────┘

Transactions Table (Optional):
┌──────────────┬────────┬──────────────┬────────────────────┐
│ AccountId    │ Type   │ Amount       │ TransactionDate    │
├──────────────┼────────┼──────────────┼────────────────────┤
│ 123...       │ Deposit│ $500         │ 2024-01-02         │
│ 123...       │ Withdr │ $300         │ 2024-01-03         │
└──────────────┴────────┴──────────────┴────────────────────┘
```

---

## Event Sourcing Approach

### Storage Structure
```sql
CREATE TABLE Events (
    EventId UNIQUEIDENTIFIER PRIMARY KEY,
    AggregateId UNIQUEIDENTIFIER,
    EventType NVARCHAR(100),
    EventData NVARCHAR(MAX),  -- JSON
    Version INT,
    OccurredAt DATETIME,
    INDEX IX_AggregateId_Version (AggregateId, Version)
)
```

### C# Code (Event Sourcing)
```csharp
// 1. Events
public class MoneyDeposited : DomainEvent
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }
}

public class MoneyWithdrawn : DomainEvent
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }
}

// 2. Aggregate
public class BankAccount
{
    // Current state (rebuilt from events)
    public Guid Id { get; private set; }
    public string AccountHolder { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsClosed { get; private set; }
    
    // Uncommitted events
    private List<DomainEvent> _uncommittedEvents = new();
    
    public void Deposit(decimal amount, string description)
    {
        // Validate
        if (IsClosed)
            throw new Exception("Account is closed");
        
        // Create event (describes what happened)
        var @event = new MoneyDeposited
        {
            AccountId = Id,
            Amount = amount,
            Description = description
        };
        
        // Apply event to update state
        Apply(@event);
        
        // Track for persistence
        _uncommittedEvents.Add(@event);
    }
    
    private void Apply(MoneyDeposited @event)
    {
        Balance += @event.Amount;
    }
    
    public void Withdraw(decimal amount, string description)
    {
        if (IsClosed)
            throw new Exception("Account is closed");
            
        if (Balance < amount)
            throw new Exception("Insufficient funds");
        
        var @event = new MoneyWithdrawn
        {
            AccountId = Id,
            Amount = amount,
            Description = description
        };
        
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
    
    private void Apply(MoneyWithdrawn @event)
    {
        Balance -= @event.Amount;
    }
    
    // Rebuild from history
    public static BankAccount LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        var account = new BankAccount();
        foreach (var @event in events)
        {
            account.Apply(@event);
        }
        return account;
    }
}
```

### What Gets Stored (Event Sourcing)
```
Events Table:
┌──────────────┬─────────────────────┬──────────────────────────────┬─────────┐
│ AggregateId  │ EventType           │ EventData                    │ Version │
├──────────────┼─────────────────────┼──────────────────────────────┼─────────┤
│ 123...       │ AccountCreated      │ {"Holder":"John","Bal":1000} │ 1       │
│ 123...       │ MoneyDeposited      │ {"Amount":500,"Desc":"..."}  │ 2       │
│ 123...       │ MoneyWithdrawn      │ {"Amount":300,"Desc":"..."}  │ 3       │
│ 123...       │ MoneyDeposited      │ {"Amount":200,"Desc":"..."}  │ 4       │
└──────────────┴─────────────────────┴──────────────────────────────┴─────────┘

Current state (Balance: $1400) is calculated by replaying all events:
$1000 + $500 - $300 + $200 = $1400
```

---

## Feature Comparison

### 1. Getting Current Balance

**Traditional:**
```csharp
var account = await _db.Accounts.FindAsync(accountId);
var balance = account.Balance;  // Instant, one query
```

**Event Sourcing:**
```csharp
var events = await _eventStore.GetEventsAsync(accountId);
var account = BankAccount.LoadFromHistory(events);
var balance = account.Balance;  // Replay all events
```

**Winner:** Traditional (for simple current state)  
**Note:** Event Sourcing can use projections to match this speed

---

### 2. Getting Balance at Specific Date

**Traditional:**
```csharp
// Need to sum all transactions up to that date
var transactions = await _db.Transactions
    .Where(t => t.AccountId == accountId && t.Date <= specificDate)
    .ToListAsync();

var balance = account.InitialBalance + 
    transactions.Where(t => t.Type == "Deposit").Sum(t => t.Amount) -
    transactions.Where(t => t.Type == "Withdrawal").Sum(t => t.Amount);
```

**Event Sourcing:**
```csharp
var events = await _eventStore.GetEventsAsync(accountId);
var historicalEvents = events.Where(e => e.OccurredAt <= specificDate);
var account = BankAccount.LoadFromHistory(historicalEvents);
var balance = account.Balance;  // Perfect historical state
```

**Winner:** Event Sourcing (built-in feature)

---

### 3. Audit Trail

**Traditional:**
```csharp
// Need to maintain separate audit table
CREATE TABLE AuditLog (
    Id INT IDENTITY,
    TableName NVARCHAR(100),
    Action NVARCHAR(50),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    ChangedBy NVARCHAR(100),
    ChangedAt DATETIME
)

// Or use database triggers
CREATE TRIGGER AuditAccountChanges
ON Accounts
AFTER UPDATE
AS BEGIN
    -- Insert into audit log
END
```

**Event Sourcing:**
```csharp
// Built-in! All events are the audit trail
var events = await _eventStore.GetEventsAsync(accountId);

// Every event shows:
// - What happened
// - When it happened  
// - What data changed
```

**Winner:** Event Sourcing (automatic, complete audit trail)

---

### 4. Debugging Production Issues

**Traditional:**
```csharp
// Issue: Balance is $1000 but should be $1500
// Problem: You only see current state
// Solution: Check logs (if you have them), database backups, etc.

// What you know:
// - Current balance: $1000
// - Expected: $1500
// - Lost information: What transactions happened?
```

**Event Sourcing:**
```csharp
// Issue: Balance is $1000 but should be $1500
var events = await _eventStore.GetEventsAsync(accountId);

// You can see EXACTLY what happened:
// 1. AccountCreated: $500
// 2. MoneyDeposited: $500 (balance: $1000)
// 3. MoneyDeposited: $300 (balance: $1300)
// 4. MoneyWithdrawn: $800 (balance: $500) ← Suspicious!
// 5. MoneyDeposited: $500 (balance: $1000)

// Easy to spot the problem!
```

**Winner:** Event Sourcing (complete history for debugging)

---

### 5. Performance - Write Operations

**Traditional:**
```csharp
// Single UPDATE query
UPDATE Accounts 
SET Balance = Balance + 500, LastModified = GETDATE()
WHERE Id = '123...'

// Fast: One query, one row update
```

**Event Sourcing:**
```csharp
// Append new event to event store
INSERT INTO Events (EventId, AggregateId, EventType, EventData, Version)
VALUES (NEWID(), '123...', 'MoneyDeposited', '{"Amount":500}', 4)

// Fast: Append-only, no updates
// But also need to check concurrency (Version)
```

**Winner:** Roughly equal (both are fast)

---

### 6. Performance - Read Operations

**Traditional:**
```csharp
// Get current state
SELECT * FROM Accounts WHERE Id = '123...'

// Very fast: Single query, one row
```

**Event Sourcing (without projections):**
```csharp
// Get all events and replay
SELECT * FROM Events WHERE AggregateId = '123...' ORDER BY Version
// Then replay all events in code

// Slow for many events (but can use snapshots)
```

**Event Sourcing (with projections):**
```csharp
// Get from read model
SELECT * FROM AccountSummaries WHERE AccountId = '123...'

// Fast: Same as traditional
```

**Winner:** Traditional (simpler), but Event Sourcing can match with projections

---

### 7. Handling Concurrent Updates

**Traditional:**
```csharp
// Two users try to update same account simultaneously
// User A: Deposit $500
// User B: Withdraw $300

// Without proper locking:
// - Both read balance: $1000
// - User A writes: $1500
// - User B writes: $700 (Wrong! Should be $1200)

// Solution: Use optimistic/pessimistic locking
// - Add RowVersion column
// - Use database transactions
```

**Event Sourcing:**
```csharp
// Built-in optimistic concurrency with version numbers
// User A loads events (version 5)
// User B loads events (version 5)

// User A tries to save event with version 6
await _eventStore.SaveEventsAsync(accountId, events, expectedVersion: 5);
// ✓ Success

// User B tries to save event with version 6
await _eventStore.SaveEventsAsync(accountId, events, expectedVersion: 5);
// ✗ Fails! Expected version 5 but current is 6

// User B must reload and retry
```

**Winner:** Event Sourcing (built-in concurrency handling)

---

### 8. Schema Changes

**Traditional:**
```csharp
// Need to add "account type" field
ALTER TABLE Accounts 
ADD AccountType NVARCHAR(50)

// Update existing rows
UPDATE Accounts SET AccountType = 'Checking'

// Update application code
public class BankAccount 
{
    public string AccountType { get; set; }  // New field
}
```

**Event Sourcing:**
```csharp
// Add new event type
public class AccountTypeChanged : DomainEvent
{
    public string AccountType { get; init; }
}

// Update aggregate
private void Apply(AccountTypeChanged @event)
{
    AccountType = @event.AccountType;
}

// Old events still work!
// No database migration needed
// Can add "AccountType" to read models only
```

**Winner:** Event Sourcing (easier evolution)

---

### 9. Testing

**Traditional:**
```csharp
[Test]
public async Task Deposit_ShouldIncreaseBalance()
{
    // Setup database
    var db = CreateTestDatabase();
    db.Accounts.Add(new Account { Id = id, Balance = 1000 });
    await db.SaveChangesAsync();
    
    // Act
    var service = new BankAccountService(db);
    await service.Deposit(id, 500);
    
    // Assert
    var account = await db.Accounts.FindAsync(id);
    Assert.That(account.Balance, Is.EqualTo(1500));
}
```

**Event Sourcing:**
```csharp
[Test]
public void Deposit_ShouldIncreaseBalance()
{
    // Setup - Pure objects, no database
    var account = new BankAccount(id, "John", 1000);
    
    // Act
    account.Deposit(500, "Salary");
    
    // Assert
    Assert.That(account.Balance, Is.EqualTo(1500));
    Assert.That(account.UncommittedEvents.Last(), Is.TypeOf<MoneyDeposited>());
}
```

**Winner:** Event Sourcing (easier unit testing, no database needed)

---

### 10. Business Intelligence

**Traditional:**
```csharp
// Analyze deposit patterns
// Need to query transaction history
SELECT 
    AccountId,
    COUNT(*) as DepositCount,
    AVG(Amount) as AvgDeposit,
    SUM(Amount) as TotalDeposits
FROM Transactions
WHERE Type = 'Deposit'
    AND TransactionDate >= '2024-01-01'
GROUP BY AccountId
```

**Event Sourcing:**
```csharp
// Same analysis from events
var depositEvents = await _eventStore.GetAllEventsAsync()
    .Where(e => e is MoneyDeposited && e.OccurredAt >= specificDate);

// Can also create specialized projections
public class DepositAnalytics
{
    public void Handle(MoneyDeposited @event)
    {
        // Update analytics in real-time
    }
}
```

**Winner:** Event Sourcing (richer data, multiple views possible)

---

## Summary Table

| Feature | Traditional | Event Sourcing | Winner |
|---------|-------------|----------------|--------|
| Current State Query | ⚡ Fast | 🐢 Slow (without projection) | Traditional |
| Historical State Query | 😰 Complex | ✅ Built-in | Event Sourcing |
| Audit Trail | 😰 Manual | ✅ Automatic | Event Sourcing |
| Debugging | 😰 Limited | ✅ Complete | Event Sourcing |
| Write Performance | ⚡ Fast | ⚡ Fast | Tie |
| Read Performance | ⚡ Fast | ⚡ Fast (with projection) | Traditional (simpler) |
| Concurrency | 😰 Manual | ✅ Built-in | Event Sourcing |
| Schema Changes | 😰 Migrations | ✅ Flexible | Event Sourcing |
| Testing | 😰 Need DB | ✅ Pure code | Event Sourcing |
| Complexity | ✅ Simple | 😰 Complex | Traditional |
| Learning Curve | ✅ Easy | 😰 Steep | Traditional |
| Business Intelligence | ✅ Good | ✅ Excellent | Event Sourcing |

---

## When to Use Each

### Use Traditional CRUD When:
- ✅ Simple CRUD operations
- ✅ No audit requirements
- ✅ Current state is all you need
- ✅ Team is not familiar with Event Sourcing
- ✅ Rapid development needed
- ✅ Low complexity requirements

### Use Event Sourcing When:
- ✅ Need complete audit trail
- ✅ Historical queries are important
- ✅ Complex business rules
- ✅ Debugging production is critical
- ✅ Multiple views of same data
- ✅ Time-based analytics needed
- ✅ High value of historical data

---

## Migration Strategy

Can you migrate from Traditional to Event Sourcing?

**Yes, but gradually:**

```csharp
// Step 1: Add event publishing to existing code
public async Task Deposit(decimal amount)
{
    // Traditional update
    account.Balance += amount;
    await _db.SaveChangesAsync();
    
    // NEW: Publish event
    await _eventBus.Publish(new MoneyDeposited(account.Id, amount));
}

// Step 2: Build projections from events
// (Initially from published events, not replayed)

// Step 3: Gradually move to event-sourced aggregates
// (New features use Event Sourcing)

// Step 4: Optionally migrate old data
// (Create events retroactively from current state)
```

---

## Conclusion

**Event Sourcing is powerful but not a silver bullet.**

- Use it when benefits outweigh complexity
- Start simple, add Event Sourcing where needed
- Can use hybrid approach (Event Sourcing for some aggregates, traditional for others)
- Team expertise and requirements should guide decision

**Remember:** Most applications don't need Event Sourcing. But when you do need it, it's invaluable!
