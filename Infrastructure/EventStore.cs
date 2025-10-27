using EventSourcingDemo.Domain.Events;

namespace EventSourcingDemo.Infrastructure;

/// <summary>
/// Interface for Event Store - stores and retrieves events
/// </summary>
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion);
    Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId);
    Task<IEnumerable<DomainEvent>> GetAllEventsAsync();
}

/// <summary>
/// In-Memory Event Store implementation
/// In production, you would use a real database (SQL Server, PostgreSQL, EventStoreDB, etc.)
/// </summary>
public class InMemoryEventStore : IEventStore
{
    // Store events by aggregate ID
    private readonly Dictionary<Guid, List<DomainEvent>> _events = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Save events for an aggregate
    /// </summary>
    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion)
    {
        lock (_lock)
        {
            if (!_events.ContainsKey(aggregateId))
            {
                _events[aggregateId] = new List<DomainEvent>();
            }
            
            var existingEvents = _events[aggregateId];
            
            // Optimistic concurrency check
            var currentVersion = existingEvents.Any() ? existingEvents.Max(e => e.Version) : 0;
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Concurrency conflict: Expected version {expectedVersion}, but current version is {currentVersion}");
            }
            
            // Add new events
            existingEvents.AddRange(events);
            
            // Log events being saved (for demonstration)
            foreach (var @event in events)
            {
                Console.WriteLine($"[EVENT STORED] {DateTime.Now:HH:mm:ss} - {@event.GetType().Name} v{@event.Version} for Aggregate {aggregateId}");
            }
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Get all events for a specific aggregate
    /// </summary>
    public Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        lock (_lock)
        {
            if (_events.TryGetValue(aggregateId, out var events))
            {
                return Task.FromResult(events.OrderBy(e => e.Version).AsEnumerable());
            }
            
            return Task.FromResult(Enumerable.Empty<DomainEvent>());
        }
    }
    
    /// <summary>
    /// Get all events from all aggregates (useful for projections)
    /// </summary>
    public Task<IEnumerable<DomainEvent>> GetAllEventsAsync()
    {
        lock (_lock)
        {
            var allEvents = _events.Values
                            .SelectMany(e => e)
                            .OrderBy(e => e.OccurredAt)
                            .ThenBy(e => e.Version)
                            .AsEnumerable();
             return Task.FromResult(allEvents);
        }
    }
    
    /// <summary>
    /// Helper method to get event count for demonstration
    /// </summary>
    public int GetTotalEventCount()
    {
        lock (_lock)
        {
            return _events.Values.Sum(e => e.Count);
        }
    }
}
