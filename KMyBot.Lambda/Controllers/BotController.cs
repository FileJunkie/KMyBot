using KMyBot.Lambda.Filters;
using KMyBot.Lambda.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace KMyBot.Lambda.Controllers;

[Route("api/[controller]")]
public class BotController : ControllerBase
{
    [HttpPost]
    [ValidateTelegramBot]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        [FromServices] UpdateHandlers handleUpdateService,
        CancellationToken cancellationToken)
    {
        await handleUpdateService.HandleUpdateAsync(update, cancellationToken);
        return Ok();
    }
}