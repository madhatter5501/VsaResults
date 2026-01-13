using VsaResults.Sample.WebApi.Models;
using VsaResults.Sample.WebApi.Services;

using Microsoft.AspNetCore.Mvc;

namespace VsaResults.Sample.WebApi.Controllers;

/// <summary>
/// Demonstrates complex ErrorOr patterns in MVC Controllers.
/// Shows async operations, multiple error aggregation, and custom error handling.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>
    /// Simple: Get order by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public ActionResult<Order> GetById(Guid id) =>
        orderService.GetById(id).ToOkResult();

    /// <summary>
    /// Medium: Get orders for a user.
    /// Demonstrates error propagation from nested service call.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public ActionResult<List<Order>> GetByUserId(Guid userId) =>
        orderService.GetByUserId(userId).ToOkResult();

    /// <summary>
    /// Complex: Create order with full validation.
    /// Demonstrates async ErrorOr, multiple error aggregation, and cross-service validation.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request) =>
        (await orderService.CreateAsync(request)).ToCreatedResult(order => $"/api/orders/{order.Id}");

    /// <summary>
    /// Complex: Cancel order with state validation.
    /// Demonstrates business rule validation based on current state.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public ActionResult<Order> Cancel(Guid id) =>
        orderService.Cancel(id).ToOkResult();

    /// <summary>
    /// Complex: Update order status with transition validation.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public ActionResult<Order> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request) =>
        orderService.UpdateStatus(id, request.Status).ToOkResult();

    /// <summary>
    /// Advanced pattern: Custom error response with context.
    /// Use Match when you need to add extra information to the response.
    /// </summary>
    [HttpGet("{id:guid}/summary")]
    public IActionResult GetSummary(Guid id) =>
        orderService.GetById(id).Match<IActionResult>(
            order => Ok(new OrderSummary(
                order.Id,
                order.Status.ToString(),
                order.Items.Count,
                order.Total,
                order.CreatedAt)),
            errors => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order Not Found",
                Detail = errors[0].Description,
                Extensions =
                {
                    ["correlationId"] = HttpContext.TraceIdentifier,
                    ["errorCode"] = errors[0].Code,
                },
            }));
}

public record UpdateStatusRequest(OrderStatus Status);

public record OrderSummary(
    Guid OrderId,
    string Status,
    int ItemCount,
    decimal Total,
    DateTime CreatedAt);
