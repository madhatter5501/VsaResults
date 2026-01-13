using VsaResults.Sample.WebApi.Controllers;
using VsaResults.Sample.WebApi.Models;
using VsaResults.Sample.WebApi.Services;

namespace VsaResults.Sample.WebApi.Endpoints;

/// <summary>
/// Demonstrates complex ErrorOr patterns with Minimal APIs.
/// Includes async operations, complex validation, and custom error handling.
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/minimal/orders")
            .WithTags("Orders (Minimal API)");

        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/user/{userId:guid}", GetByUserId);
        group.MapPost("/", CreateAsync);
        group.MapPost("/{id:guid}/cancel", Cancel);
        group.MapPatch("/{id:guid}/status", UpdateStatus);
        group.MapGet("/{id:guid}/detailed", GetDetailed);
    }

    private static IResult GetById(Guid id, IOrderService svc) =>
        svc.GetById(id).Match(Results.Ok, e => e.ToResults());

    private static IResult GetByUserId(Guid userId, IOrderService svc) =>
        svc.GetByUserId(userId).Match(Results.Ok, e => e.ToResults());

    private static async Task<IResult> CreateAsync(CreateOrderRequest req, IOrderService svc) =>
        (await svc.CreateAsync(req)).Match(
            order => Results.Created($"/api/minimal/orders/{order.Id}", order),
            e => e.ToResults());

    private static IResult Cancel(Guid id, IOrderService svc) =>
        svc.Cancel(id).Match(Results.Ok, e => e.ToResults());

    private static IResult UpdateStatus(Guid id, UpdateStatusRequest req, IOrderService svc) =>
        svc.UpdateStatus(id, req.Status).Match(Results.Ok, e => e.ToResults());

    /// <summary>
    /// Advanced: Custom error handling with enriched response.
    /// </summary>
    private static IResult GetDetailed(Guid id, IOrderService svc, HttpContext ctx) =>
        svc.GetById(id).Match(
            order => Results.Ok(new DetailedOrderResponse(
                order,
                $"Order for ${order.Total:N2}",
                DateTime.UtcNow - order.CreatedAt)),
            errors => Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order Not Found",
                Detail = errors[0].Description,
                Instance = ctx.Request.Path,
                Extensions =
                {
                    ["traceId"] = ctx.TraceIdentifier,
                    ["errorCode"] = errors[0].Code,
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                },
            }));
}

public record DetailedOrderResponse(Order Order, string Summary, TimeSpan Age);
