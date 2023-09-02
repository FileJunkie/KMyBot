using KMyBot.Lambda.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMyBot.Lambda.Controllers;

[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserDataService _userDataService;
    private readonly AuthorizationService _authorizationService;

    public AuthController(UserDataService userDataService, AuthorizationService authorizationService)
    {
        _userDataService = userDataService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await _authorizationService.GetRefreshTokenAsync(code);
            await _userDataService.SaveRefreshTokenAsync(state, refreshToken, cancellationToken);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }

        return Ok("OK that's it");
    }
}