namespace EventSourcingDemo.Domain.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public int Version { get; set; } // Version of the aggregate when event occurred
}

/// <summary>
/// Event: A new bank account was created
/// </summary>
public class AccountCreated : DomainEvent
{
    public Guid AccountId { get; init; }
    public string AccountHolder { get; init; }
    public decimal InitialBalance { get; init; }
    
    public AccountCreated(Guid accountId, string accountHolder, decimal initialBalance)
    {
        AccountId = accountId;
        AccountHolder = accountHolder;
        InitialBalance = initialBalance;
    }
}

/// <summary>
/// Event: Money was deposited into an account
/// </summary>
public class MoneyDeposited : DomainEvent
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }
    
    public MoneyDeposited(Guid accountId, decimal amount, string description)
    {
        AccountId = accountId;
        Amount = amount;
        Description = description;
    }
}

/// <summary>
/// Event: Money was withdrawn from an account
/// </summary>
public class MoneyWithdrawn : DomainEvent
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; }
    
    public MoneyWithdrawn(Guid accountId, decimal amount, string description)
    {
        AccountId = accountId;
        Amount = amount;
        Description = description;
    }
}

/// <summary>
/// Event: Account was closed
/// </summary>
public class AccountClosed : DomainEvent
{
    public Guid AccountId { get; init; }
    public string Reason { get; init; }
    
    public AccountClosed(Guid accountId, string reason)
    {
        AccountId = accountId;
        Reason = reason;
    }
}
