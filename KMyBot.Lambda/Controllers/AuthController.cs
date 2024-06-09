using KMyBot.Lambda.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMyBot.Lambda.Controllers;

[Route("api/[controller]")]
public class AuthController(UserDataService userDataService, AuthorizationService authorizationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await authorizationService.GetRefreshTokenAsync(code, cancellationToken);
            await userDataService.SaveRefreshTokenAsync(state, refreshToken, cancellationToken);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }

        return Ok("OK that's it");
    }
}