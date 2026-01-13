using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Services;

public interface IProductService
{
    ErrorOr<Product> GetById(Guid id);
    ErrorOr<List<Product>> GetAll();
    ErrorOr<Product> ReserveStock(Guid productId, int quantity);
}

public class ProductService : IProductService
{
    private static readonly Dictionary<Guid, Product> _products = new()
    {
        [Guid.Parse("aaaa1111-1111-1111-1111-111111111111")] = new Product(
            Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
            "Laptop",
            "High-performance laptop",
            999.99m,
            10),
        [Guid.Parse("bbbb2222-2222-2222-2222-222222222222")] = new Product(
            Guid.Parse("bbbb2222-2222-2222-2222-222222222222"),
            "Keyboard",
            "Mechanical keyboard",
            149.99m,
            50),
        [Guid.Parse("cccc3333-3333-3333-3333-333333333333")] = new Product(
            Guid.Parse("cccc3333-3333-3333-3333-333333333333"),
            "Mouse",
            "Wireless mouse",
            49.99m,
            0), // Out of stock for demo
    };

    public ErrorOr<Product> GetById(Guid id) =>
        _products.TryGetValue(id, out var product)
            ? product
            : DomainErrors.Product.NotFound(id);

    public ErrorOr<List<Product>> GetAll() =>
        _products.Values.ToList();

    public ErrorOr<Product> ReserveStock(Guid productId, int quantity) =>
        GetById(productId)
            .Then<Product>(product =>
            {
                if (product.StockQuantity == 0)
                {
                    return DomainErrors.Product.OutOfStock(productId);
                }

                if (product.StockQuantity < quantity)
                {
                    return DomainErrors.Order.InsufficientStock(productId, quantity, product.StockQuantity);
                }

                var updatedProduct = product with
                {
                    StockQuantity = product.StockQuantity - quantity,
                };

                _products[productId] = updatedProduct;
                return updatedProduct;
            });
}
