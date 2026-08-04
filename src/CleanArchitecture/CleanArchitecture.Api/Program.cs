using ArchitecturePatterns.Observability;
using ArchitecturePatterns.SharedKernel;
using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddArchitectureObservability();
builder.Services.AddCleanArchitecture();

var app = builder.Build();
app.UseCorrelationId();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready");
app.MapGet("/architecture", () => new { name = "Clean Architecture", dependencyRule = "Domain <- Application <- Infrastructure <- API" });
app.MapPost("/customers", async (CreateCustomerRequest request, CleanCommerce service, CancellationToken ct) => ToHttpValue(await service.CreateCustomer(request, ct)));
app.MapPost("/orders", async (CreateOrderRequest request, CleanCommerce service, CancellationToken ct) => ToHttpValue(await service.CreateOrder(request, ct)));
app.MapPost("/orders/{id:guid}/items", async (Guid id, AddOrderItemRequest request, CleanCommerce service, CancellationToken ct) => ToHttpValue(await service.AddItem(id, request, ct)));
app.MapPost("/orders/{id:guid}/confirm", async (Guid id, CleanCommerce service, CancellationToken ct) => ToHttpValue(await service.Confirm(id, ct)));
app.MapPost("/orders/{id:guid}/payments", async (Guid id, RegisterPaymentRequest request, CleanCommerce service, CancellationToken ct) => ToHttpValue(await service.RegisterPayment(id, request, ct)));
app.MapGet("/orders/{id:guid}", async (Guid id, CleanCommerce service, CancellationToken ct) => ToHttpValue(await service.GetOrder(id, ct)));
app.MapGet("/outbox", async (CleanCommerce service, CancellationToken ct) => await service.GetOutbox(ct));
app.MapPost("/outbox/dispatch", async (CleanCommerce service, CancellationToken ct) => ToHttpResult(await service.DispatchOutbox(ct)));
app.MapGet("/metrics", () => "# ArchitecturePatterns demo metrics\narchitecture_info 1\n");
app.Run();

static IResult ToHttpResult(Result result) => result.IsSuccess ? Results.Ok() : Problem(result.Error);
static IResult ToHttpValue<T>(Result<T> result) => result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);
static IResult Problem(Error error) => error.Code switch
{
    ErrorCodes.NotFound => Results.NotFound(new { error.Code, error.Message }),
    ErrorCodes.Validation => Results.BadRequest(new { error.Code, error.Message }),
    ErrorCodes.Conflict => Results.Conflict(new { error.Code, error.Message }),
    _ => Results.Problem(title: "Request failed", detail: error.Message, statusCode: 422)
};

