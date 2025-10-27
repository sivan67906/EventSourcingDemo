using EventSourcingDemo.Domain.Events;

namespace EventSourcingDemo.Domain.Aggregates;

/// <summary>
/// Bank Account Aggregate - uses Event Sourcing pattern
/// State is rebuilt by replaying events
/// </summary>
public class BankAccount
{
    // Current state
    public Guid Id { get; private set; }
    public string AccountHolder { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public bool IsClosed { get; private set; }
    public int Version { get; private set; }
    
    // Uncommitted events (not yet persisted)
    private readonly List<DomainEvent> _uncommittedEvents = new();
    public IReadOnlyList<DomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    /// <summary>
    /// Constructor for creating a new account (generates new events)
    /// </summary>
    public BankAccount(Guid id, string accountHolder, decimal initialBalance)
    {
        // Validate business rules
        if (string.IsNullOrWhiteSpace(accountHolder))
            throw new ArgumentException("Account holder name is required");
        
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative");
        
        // Create and apply the event
        var @event = new AccountCreated(id, accountHolder, initialBalance);
        ApplyEvent(@event);
    }
    
    /// <summary>
    /// Constructor for rebuilding from events (used when loading from event store)
    /// </summary>
    private BankAccount()
    {
        // Empty constructor for rebuilding from events
    }
    
    /// <summary>
    /// Deposit money into the account
    /// </summary>
    public void Deposit(decimal amount, string description)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot deposit into a closed account");
        
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive");
        
        var @event = new MoneyDeposited(Id, amount, description);
        ApplyEvent(@event);
    }
    
    /// <summary>
    /// Withdraw money from the account
    /// </summary>
    public void Withdraw(decimal amount, string description)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot withdraw from a closed account");
        
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive");
        
        if (Balance < amount)
            throw new InvalidOperationException($"Insufficient funds. Balance: {Balance}, Requested: {amount}");
        
        var @event = new MoneyWithdrawn(Id, amount, description);
        ApplyEvent(@event);
    }
    
    /// <summary>
    /// Close the account
    /// </summary>
    public void Close(string reason)
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is already closed");
        
        if (Balance != 0)
            throw new InvalidOperationException("Cannot close account with non-zero balance");
        
        var @event = new AccountClosed(Id, reason);
        ApplyEvent(@event);
    }
    
    /// <summary>
    /// Apply an event and add to uncommitted events
    /// </summary>
    private void ApplyEvent(DomainEvent @event)
    {
        @event.Version = Version + 1;
        Apply(@event); // Update state
        _uncommittedEvents.Add(@event); // Track for persistence
    }
    
    /// <summary>
    /// Apply event to update internal state (used for both new events and replay)
    /// </summary>
    private void Apply(DomainEvent @event)
    {
        // Pattern matching to handle different event types
        switch (@event)
        {
            case AccountCreated e:
                Id = e.AccountId;
                AccountHolder = e.AccountHolder;
                Balance = e.InitialBalance;
                break;
                
            case MoneyDeposited e:
                Balance += e.Amount;
                break;
                
            case MoneyWithdrawn e:
                Balance -= e.Amount;
                break;
                
            case AccountClosed e:
                IsClosed = true;
                break;
        }
        
        Version = @event.Version;
    }
    
    /// <summary>
    /// Mark all uncommitted events as committed
    /// </summary>
    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }
    
    /// <summary>
    /// Rebuild aggregate from historical events (Event Replay)
    /// </summary>
    public static BankAccount LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        var account = new BankAccount();
        
        foreach (var @event in events.OrderBy(e => e.Version))
        {
            account.Apply(@event);
        }
        
        return account;
    }
}
