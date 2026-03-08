# Mini Bank

An in-memory banking service implemented as a C# class library. No HTTP, no database, no console — the test suite is the entry point.

## What it does

- Create accounts with an initial deposit
- Deposit and withdraw funds
- Transfer funds between accounts
- Query account balance

## Running the tests

```bash
dotnet test
```

## Project structure

```
BankingService/          # Class library
  Domain/                # Money, Account, LedgerEntry, AuditEntry, Result<T>
  Infrastructure/        # Repositories, AuditLog, IdempotencyStore, LockRegistry
  BankingService.cs      # Public API

BankingService.Tests/    # xUnit test project
```

## Design highlights

- **Balance is derived, never stored** — computed by summing ledger entries on every read
- **Idempotency on all mutating operations** — pass a `Guid` key; retries return the original result
- **Per-account locking** — concurrent operations on the same account are serialized; transfers use consistent lock ordering to prevent deadlock
- **`Result<T>` instead of exceptions** — insufficient funds, account not found, and currency mismatches are returned as failures, not thrown
- **Append-only ledger** — entries are never modified or deleted; failed transfers are reversed via a compensating entry
- **`Money` value object** — currency-aware, enforces non-negative amounts and normalized currency codes at construction
