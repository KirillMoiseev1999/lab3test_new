using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Services;
using Subscriptions.Api.Contracts;
namespace Subscriptions.Api.Controllers;


[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth) : ApiControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<ActionResult<UserResponse>> Register(AuthInput input, CancellationToken ct) =>
        StatusCode(201, await auth.RegisterAsync(input, ct));

    [HttpPost("login"), AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    public async Task<ActionResult<LoginResponse>> Login(AuthInput input, CancellationToken ct) =>
        Ok(await auth.LoginAsync(input, ct));

    [HttpPost("logout")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim(), ct);
        return NoContent();
    }
}
