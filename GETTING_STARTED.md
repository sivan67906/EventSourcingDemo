# 🚀 Getting Started with Event Sourcing

Welcome! This guide will help you learn Event Sourcing in .NET step by step.

## 📚 Learning Path

### 1. Start Here (15 minutes)
Read **README.md** - Comprehensive introduction covering:
- What is Event Sourcing
- Core concepts explained
- When to use it
- Project structure

### 2. Understand the Flow (10 minutes)
Study **DIAGRAMS.md** - Visual representations of:
- Traditional vs Event Sourcing
- Complete event flow
- Event replay process
- Component relationships
- Time travel examples

### 3. See the Comparison (20 minutes)
Read **TRADITIONAL_VS_EVENT_SOURCING.md** - Side-by-side comparison:
- Same feature, two approaches
- Feature-by-feature analysis
- When to use each approach
- Migration strategies

### 4. Run the Demo (30 minutes)
Execute the project and explore:
```bash
cd EventSourcingDemo
dotnet run
```

Watch the console output to see:
- Events being created
- State being rebuilt
- Projections being built
- Business rules in action

### 5. Study the Code (1-2 hours)
Read through the code files in this order:

#### a. Domain Layer
```
Domain/Events/Events.cs
```
- See how events are defined
- Understand immutability
- Notice past tense naming

#### b. Aggregate
```
Domain/Aggregates/BankAccount.cs
```
- How state is managed
- Command methods
- Event application
- Event replay

#### c. Infrastructure
```
Infrastructure/EventStore.cs
Infrastructure/BankAccountRepository.cs
```
- How events are stored
- Loading from events
- Saving events
- Concurrency handling

#### d. Read Models
```
ReadModels/AccountSummaryProjection.cs
```
- Building projections
- Handling events
- Query optimization

#### e. Demo Application
```
Program.cs
```
- Real-world scenarios
- Complete workflow
- Best practices

### 6. Quick Reference (Ongoing)
Keep **QUICK_REFERENCE.md** handy:
- Code templates
- Common patterns
- Decision guide
- Cheat sheet
- Common mistakes

## 🎯 Hands-On Exercises

After understanding the basics, try these exercises:

### Exercise 1: Add a New Event
Add support for transferring money between accounts.

**Steps:**
1. Create `MoneyTransferred` event
2. Add `Transfer()` method to BankAccount
3. Update the Apply method
4. Test in Program.cs

**Solution hints:**
```csharp
public class MoneyTransferred : DomainEvent
{
    public Guid FromAccountId { get; init; }
    public Guid ToAccountId { get; init; }
    public decimal Amount { get; init; }
}
```

### Exercise 2: Add a Business Rule
Prevent withdrawals that would take the balance below $100 (minimum balance).

**Steps:**
1. Modify the Withdraw method validation
2. Add test cases
3. Run and verify

### Exercise 3: Create a New Projection
Create a "Transaction History" projection that shows all transactions for an account.

**Steps:**
1. Create `TransactionHistory` class
2. Handle events (MoneyDeposited, MoneyWithdrawn)
3. Build from events
4. Display in Program.cs

### Exercise 4: Add Snapshots
Implement snapshots to improve performance for accounts with many events.

**Steps:**
1. Create `AccountSnapshot` class
2. Implement snapshot storage
3. Modify loading logic to use snapshots
4. Test with 1000+ events

### Exercise 5: Event Versioning
Modify the `MoneyDeposited` event to include a currency field.

**Steps:**
1. Create `MoneyDepositedV2` with Currency field
2. Handle both old and new versions in Apply method
3. Test event replay with mixed versions

## 🔍 Exploring the Project

### Project Structure
```
EventSourcingDemo/
├── Domain/                    ← Business logic
│   ├── Events/               ← Event definitions
│   └── Aggregates/           ← Domain entities
├── Infrastructure/           ← Technical concerns
│   ├── EventStore.cs        ← Event persistence
│   └── BankAccountRepository.cs
├── ReadModels/              ← Query optimization
│   └── AccountSummaryProjection.cs
├── Program.cs               ← Demo application
├── README.md                ← Comprehensive guide
├── DIAGRAMS.md              ← Visual explanations
├── QUICK_REFERENCE.md       ← Cheat sheet
├── TRADITIONAL_VS_EVENT_SOURCING.md  ← Comparison
└── GETTING_STARTED.md       ← This file
```

### Key Files to Understand

**Must Read:**
- `Events.cs` - Foundation of the pattern
- `BankAccount.cs` - Heart of the aggregate
- `EventStore.cs` - Event persistence
- `Program.cs` - See it in action

**Important:**
- `BankAccountRepository.cs` - Loading/saving
- `AccountSummaryProjection.cs` - Read models
- `README.md` - Concepts explained

**Reference:**
- `QUICK_REFERENCE.md` - When coding
- `DIAGRAMS.md` - When confused
- `TRADITIONAL_VS_EVENT_SOURCING.md` - When deciding

## 💡 Common Questions

### Q: How long does it take to learn?
**A:** 
- Basic understanding: 2-3 hours (read + run demo)
- Comfortable coding: 1-2 days (with exercises)
- Production ready: 1-2 weeks (with real project)

### Q: Is this production-ready code?
**A:** 
This is educational code. For production:
- Use real database (SQL Server, EventStoreDB, PostgreSQL)
- Add error handling
- Implement proper concurrency
- Add monitoring and logging
- Consider snapshots
- Add event versioning strategy

### Q: Can I use this code in my project?
**A:** 
Yes! But adapt it:
- Replace InMemoryEventStore with real database
- Add your domain events
- Implement your business rules
- Add authentication/authorization
- Add proper error handling

### Q: What's the next step after this demo?
**A:**
1. **Learn CQRS** - Pairs well with Event Sourcing
2. **Study Domain-Driven Design** - Provides context
3. **Build a real project** - Best way to learn
4. **Read production implementations** - See real patterns

### Q: Do I need to use Event Sourcing everywhere?
**A:** 
**No!** Use selectively:
- Event Sourcing for: Core business entities with complex rules
- Traditional for: Simple CRUD, lookups, configuration

### Q: What about performance?
**A:**
- **Writes**: Fast (append-only)
- **Reads**: Use projections (as fast as traditional)
- **Many events**: Use snapshots
- **Scale**: Event stores scale horizontally

## 🎓 Advanced Topics

Once comfortable with basics, explore:

### 1. CQRS (Command Query Responsibility Segregation)
- Separate write and read models
- Different databases for commands and queries
- Better scalability

### 2. Event Versioning
- Handling schema changes
- Upcasting old events
- Multiple event versions

### 3. Sagas/Process Managers
- Coordinating multiple aggregates
- Long-running business processes
- Handling failures

### 4. Event Store Technologies
- **EventStoreDB**: Purpose-built for Event Sourcing
- **Marten**: PostgreSQL document DB with Event Sourcing
- **SQL Server**: Using temporal tables
- **Azure Cosmos DB**: For cloud-native apps

### 5. Projections
- Real-time projections
- Rebuild projections
- Multiple projection types
- Projection strategies

### 6. Testing Strategies
- Testing aggregates (unit tests)
- Testing event handlers
- Integration tests
- Testing projections

## 📖 Recommended Resources

### Books
1. **"Implementing Domain-Driven Design"** - Vaughn Vernon
2. **"Domain-Driven Design Distilled"** - Vaughn Vernon
3. **"Patterns, Principles, and Practices of DDD"** - Scott Millett

### Online Resources
1. **Martin Fowler's Blog** - Event Sourcing article
2. **Greg Young's Blog** - CQRS and Event Sourcing
3. **Microsoft's CQRS Journey** - Free guide
4. **EventStoreDB Documentation** - Practical examples

### Videos
1. Search for: "Greg Young - Event Sourcing"
2. Search for: "CQRS and Event Sourcing - Udi Dahan"
3. Search for: "Domain-Driven Design fundamentals"

## 🔧 Tools and Libraries

### .NET Libraries
- **Marten** - PostgreSQL document DB with Event Sourcing
- **EventFlow** - CQRS + Event Sourcing framework
- **MassTransit** - Message bus (for event publishing)
- **MediatR** - Mediator pattern (for CQRS)

### Databases
- **EventStoreDB** - Purpose-built for Event Sourcing
- **PostgreSQL** - With JSONB for events
- **SQL Server** - Traditional but works
- **Azure Cosmos DB** - Cloud-native option

### Tools
- **LINQPad** - For querying events
- **Rider/Visual Studio** - IDEs with .NET support
- **Postman** - For API testing

## ✅ Checklist: "I Understand Event Sourcing When..."

Mark these as you achieve them:

- [ ] I can explain what Event Sourcing is to a colleague
- [ ] I understand the difference between events and commands
- [ ] I can rebuild state by replaying events
- [ ] I know when to use Event Sourcing vs traditional approach
- [ ] I can create a new event and handle it
- [ ] I understand projections and why they're useful
- [ ] I can explain optimistic concurrency
- [ ] I know the benefits and drawbacks
- [ ] I've run the demo successfully
- [ ] I've completed at least 2 exercises
- [ ] I've modified the code and it still works
- [ ] I can answer: "Why not just use database triggers?"

## 🎯 Your Next Steps

1. **Today**: 
   - Run the demo
   - Read README.md
   - Study DIAGRAMS.md

2. **This Week**:
   - Complete Exercises 1-3
   - Read TRADITIONAL_VS_EVENT_SOURCING.md
   - Experiment with the code

3. **Next Week**:
   - Start a small project using Event Sourcing
   - Read recommended book chapters
   - Watch a video tutorial

4. **This Month**:
   - Implement Event Sourcing in a real feature
   - Share knowledge with team
   - Decide where Event Sourcing fits in your architecture

## 💬 Getting Help

When you're stuck:

1. **Review the code comments** - Extensively documented
2. **Check QUICK_REFERENCE.md** - Common patterns
3. **Read TRADITIONAL_VS_EVENT_SOURCING.md** - Understand differences
4. **Study the diagrams** - Visual understanding
5. **Google specific errors** - Active community
6. **Stack Overflow** - Tag: event-sourcing, cqrs
7. **Reddit** - r/dotnet, r/programming
8. **Discord/Slack** - .NET communities

## 🎉 You're Ready!

You now have everything you need to start your Event Sourcing journey:

- ✅ Complete working example
- ✅ Comprehensive documentation
- ✅ Visual diagrams
- ✅ Comparison with traditional approach
- ✅ Quick reference guide
- ✅ Exercises to practice

**Start with the demo, experiment, and build!**

Remember: Event Sourcing is a tool, not a religion. Use it where it makes sense, and don't be afraid to use traditional approaches where they're better.

---

## 📞 Quick Links

- **Run the demo**: `dotnet run`
- **Main concepts**: [README.md](README.md)
- **Visual guide**: [DIAGRAMS.md](DIAGRAMS.md)
- **Comparison**: [TRADITIONAL_VS_EVENT_SOURCING.md](TRADITIONAL_VS_EVENT_SOURCING.md)
- **Quick reference**: [QUICK_REFERENCE.md](QUICK_REFERENCE.md)

**Happy Event Sourcing! 🚀**
