using VsaResults.Sample.WebApi.Services;

namespace VsaResults.Sample.WebApi.Endpoints;

/// <summary>
/// Demonstrates simple ErrorOr patterns with Minimal APIs.
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", (IProductService svc) =>
            svc.GetAll().Match(Results.Ok, e => e.ToResults()));

        group.MapGet("/{id:guid}", (Guid id, IProductService svc) =>
            svc.GetById(id).Match(Results.Ok, e => e.ToResults()));

        group.MapPost("/{id:guid}/reserve", (Guid id, ReserveStockRequest req, IProductService svc) =>
            svc.ReserveStock(id, req.Quantity).Match(Results.Ok, e => e.ToResults()));
    }
}

public record ReserveStockRequest(int Quantity);
