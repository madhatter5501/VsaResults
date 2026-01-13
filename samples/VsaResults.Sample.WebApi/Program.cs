using VsaResults;
using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Endpoints;
using VsaResults.Sample.WebApi.Features.Products;
using VsaResults.Sample.WebApi.Features.Users;
using VsaResults.Sample.WebApi.Messaging.Consumers;
using VsaResults.Sample.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ErrorOr Sample API",
        Version = "v1",
        Description = """
            Sample API demonstrating ErrorOr library usage with ASP.NET Core.

            ## Feature-based (VSA Pattern) - RECOMMENDED
            Using IQueryFeature/IMutationFeature interfaces with automatic pipeline execution.

            ### Minimal API with FeatureHandler
            - `/api/features/users` - User management
            - `/api/features/products` - Product management

            ### Controller with FeatureController
            - `/api/controller-features/users` - User management

            ## Legacy Examples (Direct Service Injection)

            ### Controllers (Traditional MVC)
            - `/api/users` - User management with ErrorOr
            - `/api/orders` - Order management with complex validation

            ### Minimal APIs
            - `/api/products` - Product endpoints
            - `/api/minimal/users` - User management (Minimal API style)
            - `/api/minimal/orders` - Order management (Minimal API style)

            ## Error Handling
            All endpoints return RFC 7807 Problem Details for errors:
            - 400 Bad Request - Validation errors (ValidationProblemDetails)
            - 401 Unauthorized - Authentication required
            - 403 Forbidden - Permission denied
            - 404 Not Found - Resource not found
            - 409 Conflict - Business rule conflict
            - 500 Internal Server Error - Unexpected errors
            """,
    });
});

// ===========================================
// Messaging Registration
// ===========================================
// Demonstrates integration with VsaResults.Messaging for:
// - Side Effects: Publishing events after successful mutations (notification pattern)
// - Orchestration: Sending commands during mutations (coordination pattern)
builder.Services.AddVsaMessaging(cfg =>
{
    var transport = builder.Configuration["Messaging:Transport"];

    if (string.Equals(transport, "RabbitMq", StringComparison.OrdinalIgnoreCase))
    {
        // Production transport: RabbitMQ
        // Configure via appsettings or environment variables
        cfg.UseRabbitMq(r =>
        {
            r.Host = builder.Configuration["Messaging:RabbitMq:Host"] ?? "localhost";
            r.Port = int.TryParse(builder.Configuration["Messaging:RabbitMq:Port"], out var port) ? port : 5672;
            r.Username = builder.Configuration["Messaging:RabbitMq:Username"] ?? "guest";
            r.Password = builder.Configuration["Messaging:RabbitMq:Password"] ?? "guest";
        });
    }
    else
    {
        // Default: In-memory transport for development/testing
        cfg.UseInMemoryTransport();
    }

    // Auto-discover and register consumers from this assembly
    cfg.AddConsumers<UserCreatedConsumer>();

    // Configure receive endpoints for each consumer
    // Convention: queue name derived from consumer type (kebab-case)
    cfg.ReceiveEndpoint<UserCreatedConsumer>();      // Events: user-created
    cfg.ReceiveEndpoint<SendWelcomeEmailConsumer>(); // Commands: send-welcome-email
    cfg.ReceiveEndpoint<StockLowConsumer>();         // Events: stock-low

    // Global retry policy with exponential backoff
    cfg.UseMessageRetry(r => r
        .Limit(3)
        .Exponential(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)));
});

// ===========================================
// Feature-based Registration (VSA Pattern)
// ===========================================

// Repositories (must be registered before features that depend on them)
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// Register wide event emitter (required for FeatureHandler)
// Use NullWideEventEmitter when telemetry is not needed
builder.Services.AddWideEventEmitter(NullWideEventEmitter.Instance);

// Auto-register all features from the sample assembly
// This scans for IQueryFeature<,> and IMutationFeature<,> implementations
// It also registers side effects (IFeatureSideEffects<>) for messaging integration
builder.Services.AddErrorOrFeatures<GetAllUsers.Feature>();

// ===========================================
// Legacy Service Registration
// ===========================================
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IOrderService, OrderService>();

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Map MVC Controllers
app.MapControllers();

// Map Feature-based Minimal API endpoints (recommended)
app.MapUserFeatureEndpoints();
app.MapProductFeatureEndpoints();

// Map Legacy Minimal API endpoints
app.MapProductEndpoints();
app.MapUserEndpoints();
app.MapOrderEndpoints();

// Seed some test data by creating an order
using (var scope = app.Services.CreateScope())
{
    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
    await orderService.CreateAsync(new VsaResults.Sample.WebApi.Models.CreateOrderRequest(
        Guid.Parse("22222222-2222-2222-2222-222222222222"), // Regular user
        [
            new(Guid.Parse("aaaa1111-1111-1111-1111-111111111111"), 1), // Laptop
            new(Guid.Parse("bbbb2222-2222-2222-2222-222222222222"), 2), // Keyboard x2
        ]));
}

app.Run();
