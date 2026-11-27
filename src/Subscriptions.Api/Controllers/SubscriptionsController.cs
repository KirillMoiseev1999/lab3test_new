using Microsoft.AspNetCore.Mvc;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Services;
using Subscriptions.Api.Contracts;
namespace Subscriptions.Api.Controllers;


[Route("api/v1/subscriptions")]
public sealed class SubscriptionsController(SubscriptionService service) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionResponse), 201)]
    public async Task<ActionResult<SubscriptionResponse>> Create(SubscriptionInput input, CancellationToken ct) =>
        StatusCode(201, await service.CreateAsync(UserId, input, ct));

    [HttpGet]
    [ProducesResponseType(typeof(PageResponse<SubscriptionResponse>), 200)]
    public async Task<ActionResult<PageResponse<SubscriptionResponse>>> List(
        string? status, string? page, string? pageSize, CancellationToken ct) =>
        Ok(await service.ListAsync(UserId, status, page, pageSize, ct));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubscriptionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<SubscriptionResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(UserId, id, ct));

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SubscriptionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<SubscriptionResponse>> Update(Guid id, SubscriptionInput input, CancellationToken ct) =>
        Ok(await service.UpdateAsync(UserId, id, input, ct));

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        await service.ArchiveAsync(UserId, id, ct);
        return NoContent();
    }
}
