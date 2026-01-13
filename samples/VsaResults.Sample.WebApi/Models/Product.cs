namespace VsaResults.Sample.WebApi.Models;

public record Product(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);
