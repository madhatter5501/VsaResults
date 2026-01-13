using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Products;

/// <summary>
/// Complex mutation: Reserve stock for a product.
/// Demonstrates Requirements stage for loading and validating the product exists,
/// then Mutator stage for business logic (stock check and update).
/// </summary>
public static partial class ReserveStock
{
    private static class ErrorCodes
    {
        public const string InvalidId = "Product.InvalidId";
        public const string InvalidQuantity = "Product.InvalidQuantity";
    }

    private static class ErrorMessages
    {
        public const string IdCannotBeEmpty = "Product ID cannot be empty.";
        public const string QuantityMustBePositive = "Quantity must be greater than zero.";
    }

    private static class ContextKeys
    {
        public const string ProductName = "product_name";
        public const string ReservedQuantity = "reserved_quantity";
        public const string RemainingStock = "remaining_stock";
    }

    public record Request(Guid ProductId, int Quantity);

    public class Feature(
        IFeatureValidator<Request> validator,
        IFeatureRequirements<Request> requirements,
        IFeatureMutator<Request, Product> mutator,
        IFeatureSideEffects<Request>? sideEffects = null)
        : IMutationFeature<Request, Product>
    {
        public IFeatureValidator<Request> Validator => validator;

        public IFeatureRequirements<Request> Requirements => requirements;

        public IFeatureMutator<Request, Product> Mutator => mutator;

        public IFeatureSideEffects<Request> SideEffects => sideEffects ?? NoOpSideEffects<Request>.Instance;
    }

    public class Validator : IFeatureValidator<Request>
    {
        public Task<ErrorOr<Request>> ValidateAsync(Request request, CancellationToken ct = default)
        {
            var errors = new List<Error>();

            if (request.ProductId == Guid.Empty)
            {
                errors.Add(Error.Validation(ErrorCodes.InvalidId, ErrorMessages.IdCannotBeEmpty));
            }

            if (request.Quantity <= 0)
            {
                errors.Add(Error.Validation(ErrorCodes.InvalidQuantity, ErrorMessages.QuantityMustBePositive));
            }

            return errors.Count > 0
                ? Task.FromResult<ErrorOr<Request>>(errors)
                : Task.FromResult<ErrorOr<Request>>(request);
        }
    }

    public class Requirements(IProductRepository repository) : IFeatureRequirements<Request>
    {
        private const string ProductKey = "product";

        public Task<ErrorOr<FeatureContext<Request>>> EnforceAsync(Request request, CancellationToken ct = default)
        {
            var productResult = repository.GetById(request.ProductId);

            if (productResult.IsError)
            {
                return Task.FromResult<ErrorOr<FeatureContext<Request>>>(productResult.Errors);
            }

            var context = new FeatureContext<Request> { Request = request };
            context.SetEntity(ProductKey, productResult.Value);
            context.AddContext(ContextKeys.ProductName, productResult.Value.Name);

            return Task.FromResult<ErrorOr<FeatureContext<Request>>>(context);
        }

        public static Product GetProduct(FeatureContext<Request> context) =>
            context.GetEntity<Product>(ProductKey);
    }

    public class Mutator(IProductRepository repository) : IFeatureMutator<Request, Product>
    {
        public Task<ErrorOr<Product>> ExecuteAsync(FeatureContext<Request> context, CancellationToken ct = default)
        {
            var request = context.Request;
            var product = Requirements.GetProduct(context);

            if (product.StockQuantity == 0)
            {
                return Task.FromResult<ErrorOr<Product>>(DomainErrors.Product.OutOfStock(request.ProductId));
            }

            if (product.StockQuantity < request.Quantity)
            {
                return Task.FromResult<ErrorOr<Product>>(
                    DomainErrors.Order.InsufficientStock(request.ProductId, request.Quantity, product.StockQuantity));
            }

            var updatedProduct = product with { StockQuantity = product.StockQuantity - request.Quantity };
            repository.Update(updatedProduct);

            context.AddContext(ContextKeys.ReservedQuantity, request.Quantity);
            context.AddContext(ContextKeys.RemainingStock, updatedProduct.StockQuantity);

            return Task.FromResult<ErrorOr<Product>>(updatedProduct);
        }
    }
}
