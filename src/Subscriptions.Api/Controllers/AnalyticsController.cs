using Microsoft.AspNetCore.Mvc;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Services;
namespace Subscriptions.Api.Controllers;


[Route("api/v1/analytics")]
public sealed class AnalyticsController(ExpenseService service) : ApiControllerBase
{
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(ExpensesResponse), 200)]
    public async Task<ActionResult<ExpensesResponse>> Expenses(string? from, string? to, CancellationToken ct) =>
        Ok(await service.GetAsync(UserId, from, to, ct));
}
