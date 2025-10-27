# Event Sourcing Flow Diagram

## Traditional Approach vs Event Sourcing

### Traditional Approach (CRUD):
```
┌─────────────────┐
│   User Action   │
│  (Deposit $100) │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Update DB     │
│ Balance: $1500  │  ← Only current state stored
│ (Old value lost)│
└─────────────────┘
```

### Event Sourcing Approach:
```
┌─────────────────┐
│   User Action   │
│  (Deposit $100) │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│          Aggregate (BankAccount)    │
│  1. Validate business rules         │
│  2. Create event: MoneyDeposited    │
│  3. Apply event: balance += 100     │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│          Event Store                │
│  ┌─────────────────────────────┐   │
│  │ Event 1: AccountCreated     │   │
│  │ Event 2: MoneyDeposited     │   │
│  │ Event 3: MoneyWithdrawn     │   │
│  │ Event 4: MoneyDeposited ←NEW│   │
│  └─────────────────────────────┘   │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│         Projections/Read Models     │
│  ┌─────────────────────────────┐   │
│  │ Account Summary View        │   │
│  │ Transaction History View    │   │
│  │ Analytics Dashboard         │   │
│  └─────────────────────────────┘   │
└─────────────────────────────────────┘
```

## Complete Event Sourcing Flow

```
┌───────────────────────────────────────────────────────────────┐
│                     COMMAND SIDE                              │
│                                                               │
│  1. User Command                                              │
│     ↓                                                         │
│  2. Load Aggregate (replay events from Event Store)           │
│     ↓                                                         │
│  3. Execute Business Logic                                    │
│     ↓                                                         │
│  4. Generate New Events                                       │
│     ↓                                                         │
│  5. Persist Events to Event Store                             │
│                                                               │
└───────────────────────────┬───────────────────────────────────┘
                            │
                            │ Events Published
                            │
┌───────────────────────────▼───────────────────────────────────┐
│                     EVENT STORE                               │
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Aggregate ID: 12345                                     │ │
│  │ ┌─────────────────────────────────────────────────────┐ │ │
│  │ │ v1: AccountCreated (balance: $1000)                 │ │ │
│  │ │ v2: MoneyDeposited ($500)                           │ │ │
│  │ │ v3: MoneyWithdrawn ($300)                           │ │ │
│  │ │ v4: MoneyDeposited ($200)                           │ │ │
│  │ └─────────────────────────────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
└───────────────────────────┬───────────────────────────────────┘
                            │
                            │ Event Stream
                            │
┌───────────────────────────▼───────────────────────────────────┐
│                     QUERY SIDE                                │
│                                                               │
│  Event Handlers listen to events and update projections:     │
│                                                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Account Summary │  │ Transaction     │  │  Analytics   │ │
│  │ Projection      │  │ History         │  │  Dashboard   │ │
│  │                 │  │ Projection      │  │  Projection  │ │
│  │ Balance: $1400  │  │ 4 transactions  │  │  Charts &    │ │
│  │ Status: Active  │  │ Last: $200 dep  │  │  Reports     │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

## Event Replay Process

```
┌──────────────────────────────────────────────────────────┐
│         Rebuilding Aggregate from Events                 │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  1. Create Empty Aggregate                               │
│     State: { }                                           │
│                                                          │
│  2. Apply Event 1: AccountCreated                        │
│     State: { id: 123, holder: "John", balance: $1000 }  │
│                                                          │
│  3. Apply Event 2: MoneyDeposited($500)                  │
│     State: { id: 123, holder: "John", balance: $1500 }  │
│                                                          │
│  4. Apply Event 3: MoneyWithdrawn($300)                  │
│     State: { id: 123, holder: "John", balance: $1200 }  │
│                                                          │
│  5. Apply Event 4: MoneyDeposited($200)                  │
│     State: { id: 123, holder: "John", balance: $1400 }  │
│                                                          │
│  ✓ Current State Reconstructed!                          │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

## Component Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                        YOUR APPLICATION                     │
│                                                             │
│  ┌──────────────┐         ┌──────────────────────────┐    │
│  │   Program    │────────▶│  BankAccountRepository   │    │
│  │   (Client)   │         │  - GetById()             │    │
│  └──────────────┘         │  - Save()                │    │
│                           └───────────┬──────────────┘    │
│                                       │                    │
│                                       │ Uses               │
│                                       ▼                    │
│                           ┌──────────────────────────┐    │
│                           │     EventStore           │    │
│                           │  - SaveEvents()          │    │
│                           │  - GetEvents()           │    │
│                           └───────────┬──────────────┘    │
│                                       │                    │
│                                       │ Stores             │
│                                       ▼                    │
│                           ┌──────────────────────────┐    │
│                           │   Events Collection      │    │
│                           │  [Event1, Event2, ...]   │    │
│                           └──────────────────────────┘    │
│                                                             │
│  ┌──────────────────┐             ┌──────────────────┐    │
│  │  BankAccount     │◀────────────│  Domain Events   │    │
│  │  (Aggregate)     │  Creates &  │  - AccountCreated│    │
│  │  - Deposit()     │  Applies    │  - MoneyDeposited│    │
│  │  - Withdraw()    │             │  - MoneyWithdrawn│    │
│  └──────────────────┘             └──────────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Time Travel Example

```
Current Time: Day 5
You want to see the balance on Day 3

┌─────────────────────────────────────────────────┐
│  Event Timeline                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  Day 1: AccountCreated ($1000)                  │
│  Day 2: MoneyDeposited ($500)  ← Balance: $1500│
│  Day 3: MoneyWithdrawn ($300)  ← Balance: $1200│ ◀── Replay up to here
│  Day 4: MoneyDeposited ($200)                   │
│  Day 5: MoneyWithdrawn ($100)                   │
│                                                 │
│  Result: Balance on Day 3 was $1200            │
│                                                 │
└─────────────────────────────────────────────────┘

Process:
1. Load events up to Day 3
2. Replay them in order
3. Get state at that point in time
```

## Benefits Visualization

```
┌─────────────────────────────────────────────────────────────┐
│               Traditional Database                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  AccountId: 123                                             │
│  Holder: John Doe                                           │
│  Balance: $1400                                             │
│                                                             │
│  ❌ No history                                              │
│  ❌ Can't see what happened                                 │
│  ❌ Can't rebuild past states                               │
│  ❌ No audit trail                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│               Event Sourcing                                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Event 1: AccountCreated ($1000)     - 2024-01-01 09:00    │
│  Event 2: MoneyDeposited ($500)      - 2024-01-02 14:30    │
│  Event 3: MoneyWithdrawn ($300)      - 2024-01-03 10:15    │
│  Event 4: MoneyDeposited ($200)      - 2024-01-04 16:45    │
│                                                             │
│  ✅ Complete history                                        │
│  ✅ Know exactly what happened                              │
│  ✅ Can rebuild any past state                              │
│  ✅ Full audit trail                                        │
│  ✅ Multiple views from same data                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```
