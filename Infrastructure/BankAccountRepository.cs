using EventSourcingDemo.Domain.Aggregates;
using EventSourcingDemo.Domain.Events;

namespace EventSourcingDemo.Infrastructure;

/// <summary>
/// Repository for BankAccount aggregate
/// Handles loading from events and saving events
/// </summary>
public class BankAccountRepository
{
    private readonly IEventStore _eventStore;
    
    public BankAccountRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }
    
    /// <summary>
    /// Load an account from the event store by replaying its events
    /// </summary>
    public async Task<BankAccount?> GetByIdAsync(Guid accountId)
    {
        var events = await _eventStore.GetEventsAsync(accountId);
        
        if (!events.Any())
        {
            return null; // Account doesn't exist
        }
        
        Console.WriteLine($"\n[REPLAYING] Loading account {accountId} from {events.Count()} events...");
        
        // Rebuild the account from historical events
        var account = BankAccount.LoadFromHistory(events);
        
        Console.WriteLine($"[REPLAYED] Account restored - Balance: {account.Balance:C}, Version: {account.Version}");
        
        return account;
    }
    
    /// <summary>
    /// Save an account by persisting its uncommitted events
    /// </summary>
    public async Task SaveAsync(BankAccount account)
    {
        var uncommittedEvents = account.UncommittedEvents;
        
        if (!uncommittedEvents.Any())
        {
            return; // Nothing to save
        }
        
        // Calculate expected version (version before new events)
        var expectedVersion = account.Version - uncommittedEvents.Count;
        
        // Save events to event store
        await _eventStore.SaveEventsAsync(account.Id, uncommittedEvents, expectedVersion);
        
        // Mark events as committed
        account.MarkEventsAsCommitted();
    }
}
