using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Products;

/// <summary>
/// Simple query: Get all products.
/// </summary>
public static class GetAllProducts
{
    public record Request;

    public class Feature(IFeatureQuery<Request, List<Product>> query) : IQueryFeature<Request, List<Product>>
    {
        public IFeatureQuery<Request, List<Product>> Query => query;
    }

    public class Query(IProductRepository repository) : IFeatureQuery<Request, List<Product>>
    {
        public Task<ErrorOr<List<Product>>> ExecuteAsync(Request request, CancellationToken ct = default) =>
            Task.FromResult<ErrorOr<List<Product>>>(repository.GetAll());
    }
}
