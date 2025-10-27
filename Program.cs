using EventSourcingDemo.Domain.Aggregates;
using EventSourcingDemo.Infrastructure;
using EventSourcingDemo.ReadModels;

namespace EventSourcingDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       Event Sourcing Demo - Banking System                ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        
        // Setup infrastructure
        var eventStore = new InMemoryEventStore();
        var repository = new BankAccountRepository(eventStore);
        
        // ========================================
        // Scenario 1: Create a new account
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 1: Creating a New Bank Account");
        Console.WriteLine(new string('=', 60));
        
        var accountId = Guid.NewGuid();
        var account = new BankAccount(accountId, "John Doe", 1000m);
        await repository.SaveAsync(account);
        
        Console.WriteLine($"\n✓ Account created for John Doe with initial balance: $1,000");
        
        // ========================================
        // Scenario 2: Perform transactions
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 2: Performing Transactions");
        Console.WriteLine(new string('=', 60));
        
        // Load account from events
        account = await repository.GetByIdAsync(accountId);
        
        if (account != null)
        {
            // Make deposits
            account.Deposit(500m, "Salary payment");
            account.Deposit(200m, "Freelance work");
            
            // Make withdrawals
            account.Withdraw(300m, "Rent payment");
            account.Withdraw(150m, "Groceries");
            
            // Save all changes
            await repository.SaveAsync(account);
            
            Console.WriteLine($"\n✓ Transactions completed. Current balance: ${account.Balance:N2}");
        }
        
        // ========================================
        // Scenario 3: Demonstrate Event Replay
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 3: Event Replay - Rebuilding State from Events");
        Console.WriteLine(new string('=', 60));
        
        // Load account again (will replay all events)
        var reloadedAccount = await repository.GetByIdAsync(accountId);
        
        if (reloadedAccount != null)
        {
            Console.WriteLine($"\n✓ Account state rebuilt from events:");
            Console.WriteLine($"  - Account Holder: {reloadedAccount.AccountHolder}");
            Console.WriteLine($"  - Balance: ${reloadedAccount.Balance:N2}");
            Console.WriteLine($"  - Version: {reloadedAccount.Version}");
        }
        
        // ========================================
        // Scenario 4: Create multiple accounts
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 4: Working with Multiple Accounts");
        Console.WriteLine(new string('=', 60));
        
        var account2Id = Guid.NewGuid();
        var account2 = new BankAccount(account2Id, "Jane Smith", 2000m);
        account2.Deposit(1000m, "Bonus");
        account2.Withdraw(500m, "Investment");
        await repository.SaveAsync(account2);
        
        var account3Id = Guid.NewGuid();
        var account3 = new BankAccount(account3Id, "Bob Johnson", 500m);
        account3.Deposit(300m, "Gift");
        await repository.SaveAsync(account3);
        
        Console.WriteLine("\n✓ Created 2 additional accounts");
        
        // ========================================
        // Scenario 5: Build Projections
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 5: Building Read Model Projections");
        Console.WriteLine(new string('=', 60));
        
        var projection = new AccountSummaryProjection();
        var allEvents = await eventStore.GetAllEventsAsync();
        projection.BuildFromEvents(allEvents);
        
        Console.WriteLine("\nAccount Summaries:");
        Console.WriteLine(new string('-', 60));
        
        foreach (var summary in projection.GetAllSummaries())
        {
            Console.WriteLine($"\n  Account: {summary.AccountHolder}");
            Console.WriteLine($"  Balance: ${summary.CurrentBalance:N2}");
            Console.WriteLine($"  Transactions: {summary.TotalTransactions}");
            Console.WriteLine($"  Created: {summary.CreatedAt:g}");
            Console.WriteLine($"  Status: {(summary.IsClosed ? "CLOSED" : "ACTIVE")}");
        }
        
        // ========================================
        // Scenario 6: Event History & Audit Trail
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 6: Complete Event History (Audit Trail)");
        Console.WriteLine(new string('=', 60));
        
        var accountEvents = await eventStore.GetEventsAsync(accountId);
        
        Console.WriteLine($"\nComplete event history for {reloadedAccount?.AccountHolder}:");
        Console.WriteLine(new string('-', 60));
        
        foreach (var evt in accountEvents)
        {
            Console.WriteLine($"\n  [{evt.Version}] {evt.GetType().Name}");
            Console.WriteLine($"      Time: {evt.OccurredAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"      Event ID: {evt.EventId}");
            
            // Display event-specific details
            switch (evt)
            {
                case EventSourcingDemo.Domain.Events.AccountCreated e:
                    Console.WriteLine($"      Details: Initial balance ${e.InitialBalance:N2}");
                    break;
                case EventSourcingDemo.Domain.Events.MoneyDeposited e:
                    Console.WriteLine($"      Details: +${e.Amount:N2} - {e.Description}");
                    break;
                case EventSourcingDemo.Domain.Events.MoneyWithdrawn e:
                    Console.WriteLine($"      Details: -${e.Amount:N2} - {e.Description}");
                    break;
            }
        }
        
        // ========================================
        // Scenario 7: Business Rule Validation
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 7: Business Rule Validation");
        Console.WriteLine(new string('=', 60));
        
        account = await repository.GetByIdAsync(accountId);
        
        if (account != null)
        {
            try
            {
                Console.WriteLine("\nAttempting to withdraw more than balance...");
                account.Withdraw(10000m, "Large purchase"); // This should fail
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✗ Operation rejected: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("\nAttempting to deposit negative amount...");
                var tempAccount = new BankAccount(Guid.NewGuid(), "Test", -100m); // This should fail
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"✗ Operation rejected: {ex.Message}");
            }
        }
        
        // ========================================
        // Scenario 8: Close an account
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SCENARIO 8: Closing an Account");
        Console.WriteLine(new string('=', 60));
        
        account3 = await repository.GetByIdAsync(account3Id);
        if (account3 != null)
        {
            // First withdraw all money
            account3.Withdraw(account3.Balance, "Final withdrawal");
            account3.Close("Customer request");
            await repository.SaveAsync(account3);
            
            Console.WriteLine($"\n✓ Account for Bob Johnson has been closed");
            
            // Try to deposit into closed account
            try
            {
                account3.Deposit(100m, "Attempt after close");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✗ Cannot perform operation: {ex.Message}");
            }
        }
        
        // ========================================
        // Summary
        // ========================================
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("SUMMARY");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"\nTotal Events Stored: {eventStore.GetTotalEventCount()}");
        Console.WriteLine($"Total Accounts: {projection.GetAllSummaries().Count()}");
        Console.WriteLine($"Active Accounts: {projection.GetAllSummaries().Count(a => !a.IsClosed)}");
        
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("KEY BENEFITS OF EVENT SOURCING DEMONSTRATED:");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("✓ Complete audit trail of all changes");
        Console.WriteLine("✓ Ability to rebuild state at any point in time");
        Console.WriteLine("✓ Event replay for debugging and analysis");
        Console.WriteLine("✓ Multiple projections from same events");
        Console.WriteLine("✓ Business logic validation before events");
        Console.WriteLine("✓ Immutable event history");
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
