using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Products;

public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new()
    {
        [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")] = new Product(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Widget",
            "A handy widget for all your widget needs",
            9.99m,
            100),
        [Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")] = new Product(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Gadget",
            "The latest gadget technology",
            24.99m,
            0),
    };

    public List<Product> GetAll() =>
        _products.Values.ToList();

    public ErrorOr<Product> GetById(Guid id) =>
        _products.TryGetValue(id, out var product)
            ? product
            : DomainErrors.Product.NotFound(id);

    public void Update(Product product) =>
        _products[product.Id] = product;
}
