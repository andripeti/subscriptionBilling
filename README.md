# Subscription Billing System

A small billing backend in C# / .NET 9. It handles customers, subscriptions, and invoices, with an outbox for domain events, idempotent command handling, and a background worker that generates recurring invoices. Built as a take-home, so the focus was on getting the domain model and the boring-but-important pieces (transactional outbox, idempotency, aggregate boundaries) right — not on production infra.

## Solution layout

```
SubscriptionBilling.sln
├── src/
│   ├── SubscriptionBilling.Domain          # Aggregates, value objects, events, invariants
│   ├── SubscriptionBilling.Application     # CQRS handlers, abstractions, billing service
│   ├── SubscriptionBilling.Infrastructure  # EF Core, repos, outbox, idempotency, hosted workers
│   └── SubscriptionBilling.Api             # Minimal API endpoints + DI wiring
└── tests/
    └── SubscriptionBilling.Domain.Tests    # xUnit tests for domain invariants
```

Dependencies point inward (`Api → Infrastructure → Application → Domain`). Domain has no references to anything; Application doesn't know EF Core or ASP.NET exists. Nothing fancy, just the usual layering — it mostly pays off when you want to unit-test the domain without spinning up a DbContext.

## Domain model

Three aggregates. Each one owns its own invariants and only exposes behavior through methods, not setters:

- **Customer** — `CustomerId` + an `Email` value object that trims/lowercases and validates on construction.
- **Subscription** — wraps a `Plan` (name, `Money` price, `BillingCycle`). States go `PendingActivation → Active → Cancelled`. `Activate()` issues the first invoice and stamps `NextBillingDate`. `GenerateNextInvoice(now)` issues the next one if that date has passed. `Cancel()` stops future billing but leaves existing invoices alone — cancelling shouldn't rewrite history.
- **Invoice** — `Pending → Paid`. `Pay()` is the only mutating method, and calling it twice throws. Easier to make that a hard invariant than to deal with "idempotent payment" semantics at the domain level.

`Money` is a value object with currency-checked arithmetic (adding USD to EUR throws). Aggregate roots inherit from `AggregateRoot<TId>`, which just holds a list of pending domain events that get flushed on save.

### Domain events
- `SubscriptionActivated` — raised from `Subscription.Activate`
- `InvoiceGenerated` — raised inside `Invoice.Issue` (which is called by both `Activate` and `GenerateNextInvoice`)
- `PaymentReceived` — raised from `Invoice.Pay`

## CQRS

Commands and queries are plain records implementing `ICommand<TResult>` / `IQuery<TResult>`, one handler each. No MediatR — I wired up a thin dispatcher manually because the handler count is small and it made the flow easier to read.

| Command/Query | Handler |
| --- | --- |
| `CreateCustomerCommand` | `CreateCustomerHandler` |
| `CreateSubscriptionCommand` | `CreateSubscriptionHandler` (activates → first invoice) |
| `CancelSubscriptionCommand` | `CancelSubscriptionHandler` |
| `PayInvoiceCommand` | `PayInvoiceHandler` |
| `GetInvoicesQuery` | `GetInvoicesHandler` |

`IdempotentCommandHandler<TCommand, TResult>` is a decorator around any command handler. If the request carries an `Idempotency-Key` header, the first result is stored in `IdempotencyRecords` and replayed on retries. Keys aren't scoped per-user yet — in a real system I'd scope them by caller/API key, but for the demo a flat table is fine.

## Persistence

`BillingDbContext` (EF Core 9, InMemory provider) implements `IUnitOfWork`. InMemory is obviously not a real database — I used it here so the project runs with `dotnet run` and no external dependencies. Swapping in the SQL Server or Postgres provider should mostly be a matter of changing the DI registration and adding migrations; the outbox interceptor already runs inside the same `SaveChanges` transaction, so it'll behave correctly once there's a real one.

Each repository (`ICustomerRepository`, `ISubscriptionRepository`, `IInvoiceRepository`) loads and saves a single aggregate root — no cross-aggregate queries from a repo, no lazy-loading graphs that pull in half the database. Value objects (`Email`, `Plan`, `Money`) are mapped as owned types so they live on the same table as their owner. Strongly-typed IDs go through EF value converters.

## Outbox pattern

`DomainEventsToOutboxInterceptor` hooks into `SaveChangesAsync`: before the transaction commits, it walks every tracked aggregate root, pulls its pending domain events, serializes them, and writes them to `OutboxMessages` — in the **same** transaction as the state change. That's the whole point; if the DB write rolls back, the events roll back with it, and we never publish an event for a state change that didn't happen.

`OutboxProcessor` is a hosted service that polls every 5 seconds, deserializes each pending message, and hands it to `IDomainEventDispatcher`. Right now the dispatcher just logs — in production you'd swap in whatever bus you use (Service Bus, Kafka, RabbitMQ, etc.). Polling is intentionally simple; a 5s interval is fine for a demo but a real system would want either a shorter interval or a notification channel (LISTEN/NOTIFY on Postgres, for example) to cut latency.

## Background billing job

`BillingCycleWorker` is a `BackgroundService` that wakes once a minute, pulls subscriptions where `NextBillingDate <= now`, and calls `GenerateNextInvoice` on each. All of them get persisted in one `SaveChanges`, which also enqueues the `InvoiceGenerated` outbox messages atomically.

A minute is coarse — fine for this scope, but I'd change a few things for production: run it on a single instance (or use a distributed lock / leader election), batch in pages rather than loading everything at once, and add a retry/backoff for transient DB failures. None of that is here.

## API surface

| Method | Route | Body |
| --- | --- | --- |
| POST | `/customers` | `{ "name", "email" }` |
| POST | `/subscriptions` | `{ "customerId", "planName", "priceAmount", "priceCurrency", "cycle" }` |
| POST | `/subscriptions/{id}/cancel` | — |
| POST | `/invoices/{id}/pay` | — |
| GET  | `/invoices?customerId=...&subscriptionId=...` | — |

Command endpoints honor the `Idempotency-Key` header. `DomainExceptionHandler` turns any `DomainException` into an RFC 7807 problem-details response with HTTP 400, so broken-invariant errors come back in a consistent shape instead of a 500.

Auth isn't wired up — out of scope for the exercise. In practice I'd put this behind JWT auth and scope idempotency keys (and customer lookups) to the authenticated caller.

## Running the project

**Prerequisites:** .NET 9 SDK. No external services needed — it uses EF InMemory, so no database to set up.

```bash
# 1. Clone and restore
git clone <repo-url>
cd SubscriptionBilling
dotnet restore SubscriptionBilling.sln

# 2. Run the API (starts on https://localhost:5001 by default)
dotnet run --project src/SubscriptionBilling.Api
```

Once it's up, the Swagger UI is available at **http://localhost:5000/swagger** — all endpoints are documented there and can be called directly from the browser without needing curl or a separate client.

Alternatively, try the happy path via curl:

```bash
# Create a customer
curl -s -X POST https://localhost:5001/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Andri","email":"andri@example.com"}' | jq

# Create a subscription (use the customerId from above)
curl -s -X POST https://localhost:5001/subscriptions \
  -H "Content-Type: application/json" \
  -d '{"customerId":"<id>","planName":"Pro","priceAmount":29.99,"priceCurrency":"USD","cycle":"Monthly"}' | jq

# Check invoices — the first one is generated on activation
curl -s "https://localhost:5001/invoices?customerId=<id>" | jq

# Pay the invoice
curl -s -X POST https://localhost:5001/invoices/<invoiceId>/pay | jq
```

For idempotency, add `-H "Idempotency-Key: some-uuid"` to any POST. Repeating the request with the same key returns the original result without re-executing the handler.

## Build & test

```bash
dotnet build SubscriptionBilling.sln
dotnet test SubscriptionBilling.sln
```

21 xUnit tests cover the domain: `Email` / `Customer` validation, the subscription lifecycle (activate, cancel, next-invoice generation based on `NextBillingDate`), `Invoice` payment invariants (can't pay twice, can't pay a cancelled one), and `Money` arithmetic. No integration tests against EF yet — with the InMemory provider they'd mostly test EF itself, and I'd rather add them against a real provider once one is configured.

## Things I'd change with more time

- Replace InMemory with Postgres, add migrations, and add integration tests around the outbox interceptor (the "event is written in the same transaction" property really needs a test against a real DB).
- Scope `Idempotency-Key` by caller, and expire old records.
- Swap the outbox polling loop for something push-based.
- Break `BillingCycleWorker` into paged batches and add a distributed lock so it's safe to run more than one API instance.
- Proper auth + per-customer authorization on the read endpoints.
