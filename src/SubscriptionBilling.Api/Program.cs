using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Customers.Commands;
using SubscriptionBilling.Application.Invoices.Commands;
using SubscriptionBilling.Application.Invoices.Queries;
using SubscriptionBilling.Application.Subscriptions.Commands;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBillingInfrastructure(dbName: "BillingDb");
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Subscription Billing API",
        Version = "v1",
        Description = "DDD + Clean Architecture + CQRS billing system"
    });
    c.AddServer(new OpenApiServer { Url = "http://localhost:5000" });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Subscription Billing v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Subscription Billing API";
});

// --- Customers ---
app.MapPost("/customers", async (
    CreateCustomerRequest body,
    HttpContext http,
    ICommandHandler<CreateCustomerCommand, Guid> handler,
    ICommandHandler<Idempotent<CreateCustomerCommand, Guid>, Guid> idempotentHandler,
    CancellationToken ct) =>
{
    var cmd = new CreateCustomerCommand(body.Name, body.Email);
    var id = http.Request.Headers.TryGetValue("Idempotency-Key", out var key) && !string.IsNullOrWhiteSpace(key)
        ? await idempotentHandler.Handle(new Idempotent<CreateCustomerCommand, Guid>(key.ToString(), cmd), ct)
        : await handler.Handle(cmd, ct);
    return Results.Created($"/customers/{id}", new { id });
})
.WithName("CreateCustomer")
.WithSummary("Create a new customer")
.WithDescription("Creates a customer. Supply an `Idempotency-Key` header to make the call safe to retry.")
.WithTags("Customers")
.Produces<object>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest);

// --- Subscriptions ---
app.MapPost("/subscriptions", async (
    CreateSubscriptionRequest body,
    HttpContext http,
    ICommandHandler<CreateSubscriptionCommand, CreateSubscriptionResult> handler,
    ICommandHandler<Idempotent<CreateSubscriptionCommand, CreateSubscriptionResult>, CreateSubscriptionResult> idempotentHandler,
    CancellationToken ct) =>
{
    var cmd = new CreateSubscriptionCommand(body.CustomerId, body.PlanName, body.PriceAmount, body.PriceCurrency, body.Cycle);
    var result = http.Request.Headers.TryGetValue("Idempotency-Key", out var key) && !string.IsNullOrWhiteSpace(key)
        ? await idempotentHandler.Handle(new Idempotent<CreateSubscriptionCommand, CreateSubscriptionResult>(key.ToString(), cmd), ct)
        : await handler.Handle(cmd, ct);
    return Results.Created($"/subscriptions/{result.SubscriptionId}", result);
})
.WithName("CreateSubscription")
.WithSummary("Create and activate a subscription")
.WithDescription("Creates a subscription for an existing customer, immediately activates it, and generates the first invoice.")
.WithTags("Subscriptions")
.Produces<CreateSubscriptionResult>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest);

app.MapPost("/subscriptions/{id:guid}/cancel", async (
    Guid id,
    ICommandHandler<CancelSubscriptionCommand, Unit> handler,
    CancellationToken ct) =>
{
    await handler.Handle(new CancelSubscriptionCommand(id), ct);
    return Results.NoContent();
})
.WithName("CancelSubscription")
.WithSummary("Cancel a subscription")
.WithDescription("Stops future invoice generation. Existing invoices and billing history are kept intact.")
.WithTags("Subscriptions")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status400BadRequest);

// --- Invoices ---
app.MapPost("/invoices/{id:guid}/pay", async (
    Guid id,
    HttpContext http,
    ICommandHandler<PayInvoiceCommand, Unit> handler,
    ICommandHandler<Idempotent<PayInvoiceCommand, Unit>, Unit> idempotentHandler,
    CancellationToken ct) =>
{
    var cmd = new PayInvoiceCommand(id);
    if (http.Request.Headers.TryGetValue("Idempotency-Key", out var key) && !string.IsNullOrWhiteSpace(key))
        await idempotentHandler.Handle(new Idempotent<PayInvoiceCommand, Unit>(key.ToString(), cmd), ct);
    else
        await handler.Handle(cmd, ct);
    return Results.NoContent();
})
.WithName("PayInvoice")
.WithSummary("Pay an invoice")
.WithDescription("Marks the invoice as Paid. Returns 400 if it is already paid. Safe to retry with an `Idempotency-Key` header.")
.WithTags("Invoices")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status400BadRequest);

app.MapGet("/invoices", async (
    [FromQuery] Guid? customerId,
    [FromQuery] Guid? subscriptionId,
    IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>> handler,
    CancellationToken ct) =>
{
    var result = await handler.Handle(new GetInvoicesQuery(customerId, subscriptionId), ct);
    return Results.Ok(result);
})
.WithName("GetInvoices")
.WithSummary("List invoices")
.WithDescription("Returns invoices. Supply `customerId` to list all invoices for a customer, `subscriptionId` to list for one subscription, or both to filter. At least one is required.")
.WithTags("Invoices")
.Produces<IReadOnlyList<InvoiceDto>>();

app.Run();

// --- Request / Response types ---

public sealed record CreateCustomerRequest(string Name, string Email);

public sealed record CreateSubscriptionRequest(
    Guid CustomerId,
    string PlanName,
    decimal PriceAmount,
    string PriceCurrency,
    BillingCycle Cycle);

// --- Exception handler ---

internal sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        if (ex is not DomainException domain) return false;
        http.Response.StatusCode = StatusCodes.Status400BadRequest;
        await http.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Domain rule violation",
            Detail = domain.Message,
            Status = StatusCodes.Status400BadRequest
        }, ct);
        return true;
    }
}

public partial class Program;
