namespace VsaResults.Sample.WebApi.Models;

/// <summary>
/// Centralized domain errors for the application.
/// This pattern keeps error definitions consistent and reusable.
/// </summary>
public static class DomainErrors
{
    public static class User
    {
        public static Error NotFound(Guid userId) =>
            Error.NotFound("User.NotFound", $"User with ID '{userId}' was not found.");

        public static Error DuplicateEmail(string email) =>
            Error.Conflict("User.DuplicateEmail", $"A user with email '{email}' already exists.");

        public static Error InvalidEmail(string email) =>
            Error.Validation("User.InvalidEmail", $"'{email}' is not a valid email address.");

        public static Error NameTooShort =>
            Error.Validation("User.NameTooShort", "Name must be at least 2 characters long.");

        public static Error NameTooLong =>
            Error.Validation("User.NameTooLong", "Name cannot exceed 100 characters.");

        public static Error CannotDeleteAdmin =>
            Error.Forbidden("User.CannotDeleteAdmin", "Admin users cannot be deleted.");
    }

    public static class Order
    {
        public static Error NotFound(Guid orderId) =>
            Error.NotFound("Order.NotFound", $"Order with ID '{orderId}' was not found.");

        public static Error EmptyItems =>
            Error.Validation("Order.EmptyItems", "Order must contain at least one item.");

        public static Error InvalidQuantity(Guid productId) =>
            Error.Validation("Order.InvalidQuantity", $"Quantity for product '{productId}' must be greater than zero.");

        public static Error AlreadyCancelled(Guid orderId) =>
            Error.Conflict("Order.AlreadyCancelled", $"Order '{orderId}' has already been cancelled.");

        public static Error CannotCancelShipped(Guid orderId) =>
            Error.Conflict("Order.CannotCancelShipped", $"Order '{orderId}' cannot be cancelled because it has already shipped.");

        public static Error InsufficientStock(Guid productId, int requested, int available) =>
            Error.Validation(
                "Order.InsufficientStock",
                $"Insufficient stock for product '{productId}'. Requested: {requested}, Available: {available}.");
    }

    public static class Product
    {
        public static Error NotFound(Guid productId) =>
            Error.NotFound("Product.NotFound", $"Product with ID '{productId}' was not found.");

        public static Error OutOfStock(Guid productId) =>
            Error.Conflict("Product.OutOfStock", $"Product '{productId}' is out of stock.");
    }

    public static class Auth
    {
        public static Error Unauthorized =>
            Error.Unauthorized("Auth.Unauthorized", "You must be authenticated to perform this action.");

        public static Error Forbidden =>
            Error.Forbidden("Auth.Forbidden", "You do not have permission to perform this action.");

        public static Error InvalidToken =>
            Error.Unauthorized("Auth.InvalidToken", "The provided authentication token is invalid.");

        public static Error TokenExpired =>
            Error.Unauthorized("Auth.TokenExpired", "The authentication token has expired.");
    }
}
