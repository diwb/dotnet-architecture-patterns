using ArchitecturePatterns.Observability;
using ArchitecturePatterns.SharedKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddArchitectureObservability();
builder.Services.AddSingleton<CommerceState>();
builder.Services.AddSingleton(sp => new CommerceService(sp.GetRequiredService<CommerceState>(), "modular-monolith"));

var app = builder.Build();
app.UseCorrelationId();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready");
app.MapGet("/architecture", () => new { name = "Modular Monolith", modules = new[] { "Customers", "Orders", "Inventory", "Payments" } });
app.MapPost("/customers", async (CreateCustomerRequest request, CommerceService service, CancellationToken ct) => ToHttpValue(await service.CreateCustomer(request, ct)));
app.MapPost("/orders", async (CreateOrderRequest request, CommerceService service, CancellationToken ct) => ToHttpValue(await service.CreateOrder(request, ct)));
app.MapPost("/orders/{id:guid}/items", async (Guid id, AddOrderItemRequest request, CommerceService service, CancellationToken ct) => ToHttpValue(await service.AddItem(id, request, ct)));
app.MapPost("/orders/{id:guid}/confirm", async (Guid id, CommerceService service, CancellationToken ct) => ToHttpValue(await service.Confirm(id, ct)));
app.MapPost("/orders/{id:guid}/payments", async (Guid id, RegisterPaymentRequest request, CommerceService service, CancellationToken ct) => ToHttpValue(await service.RegisterPayment(id, request, ct)));
app.MapGet("/orders/{id:guid}", async (Guid id, CommerceService service, CancellationToken ct) => ToHttpValue(await service.GetOrder(id, ct)));
app.MapGet("/outbox", async (CommerceService service, CancellationToken ct) => await service.GetOutbox(ct));
app.MapPost("/outbox/dispatch", async (CommerceService service, CancellationToken ct) => ToHttpResult(await service.DispatchOutbox(ct)));
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

