using VsaResults.Sample.WebApi.Features.Products;
using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Endpoints;

/// <summary>
/// Product endpoints using FeatureHandler.
/// </summary>
public static class ProductFeatureEndpoints
{
    public static void MapProductFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/features/products")
            .WithTags("Products (Feature-based)");

        // Simple query
        group.MapGet("/", FeatureHandler.QueryOk<GetAllProducts.Request, List<Product>>());

        // Complex mutation with stock validation
        group.MapPost("/{productId:guid}/reserve", FeatureHandler.MutationOk<ReserveStock.Request, Product>());
    }
}
