using EventSourcingDemo.Domain.Events;

namespace EventSourcingDemo.ReadModels;

/// <summary>
/// Read Model for account summary
/// This is a projection built from events for fast querying
/// </summary>
public class AccountSummary
{
    public Guid AccountId { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsClosed { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// Projection builder that creates read models from events
/// In a real system, this would update a separate read database
/// </summary>
public class AccountSummaryProjection
{
    private readonly Dictionary<Guid, AccountSummary> _summaries = new();
    
    /// <summary>
    /// Build projections from all events
    /// </summary>
    public void BuildFromEvents(IEnumerable<DomainEvent> events)
    {
        _summaries.Clear();
        
        foreach (var @event in events.OrderBy(e => e.OccurredAt).ThenBy(e => e.Version))
        {
            Apply(@event);
        }
    }
    
    /// <summary>
    /// Apply a single event to update the projection
    /// </summary>
    private void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated e:
                _summaries[e.AccountId] = new AccountSummary
                {
                    AccountId = e.AccountId,
                    AccountHolder = e.AccountHolder,
                    CurrentBalance = e.InitialBalance,
                    IsClosed = false,
                    TotalTransactions = 0,
                    CreatedAt = e.OccurredAt
                };
                break;
                
            case MoneyDeposited e:
                if (_summaries.TryGetValue(e.AccountId, out var depositSummary))
                {
                    depositSummary.CurrentBalance += e.Amount;
                    depositSummary.TotalTransactions++;
                }
                break;
                
            case MoneyWithdrawn e:
                if (_summaries.TryGetValue(e.AccountId, out var withdrawSummary))
                {
                    withdrawSummary.CurrentBalance -= e.Amount;
                    withdrawSummary.TotalTransactions++;
                }
                break;
                
            case AccountClosed e:
                if (_summaries.TryGetValue(e.AccountId, out var closedSummary))
                {
                    closedSummary.IsClosed = true;
                    closedSummary.ClosedAt = e.OccurredAt;
                }
                break;
        }
    }
    
    /// <summary>
    /// Get all account summaries
    /// </summary>
    public IEnumerable<AccountSummary> GetAllSummaries()
    {
        return _summaries.Values;
    }
    
    /// <summary>
    /// Get summary for a specific account
    /// </summary>
    public AccountSummary? GetSummary(Guid accountId)
    {
        return _summaries.TryGetValue(accountId, out var summary) ? summary : null;
    }
}
