using Microsoft.AspNetCore.Mvc;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Services;
using Subscriptions.Business.Validation;
using Subscriptions.Api.Contracts;
namespace Subscriptions.Api.Controllers;


[Route("api/v1")]
public sealed class PaymentsController(PaymentService service) : ApiControllerBase
{
    [HttpPost("subscriptions/{id}/payments")]
    [ProducesResponseType(typeof(PaymentResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<ActionResult<PaymentResponse>> Create(Guid id, PaymentInput input,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        if (Request.Headers["Idempotency-Key"].Count > 1)
            throw InputValidator.Invalid("Idempotency-Key", "Допускается один ключ.");
        return StatusCode(201, await service.CreateAsync(UserId, id, key, input, ct));
    }
    [HttpGet("payments")]
    [ProducesResponseType(typeof(PageResponse<PaymentResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<PageResponse<PaymentResponse>>> List(string? from, string? to,
        Guid? subscriptionId, string? page, string? pageSize, CancellationToken ct) =>
        Ok(await service.ListAsync(UserId, subscriptionId, from, to, page, pageSize, ct));
}
