using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Products;

public interface IProductRepository
{
    List<Product> GetAll();

    ErrorOr<Product> GetById(Guid id);

    void Update(Product product);
}
