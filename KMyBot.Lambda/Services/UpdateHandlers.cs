using Dropbox.Api;
using Dropbox.Api.Files;
using KMyBot.Common.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace KMyBot.Lambda.Services;

public class UpdateHandlers
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandlers> _logger;
    private readonly UserDataService _userDataService;
    private readonly AuthorizationService _authorizationService;

    public UpdateHandlers(ITelegramBotClient botClient, ILogger<UpdateHandlers> logger, UserDataService userDataService, AuthorizationService authorizationService)
    {
        _botClient = botClient;
        _logger = logger;
        _userDataService = userDataService;
        _authorizationService = authorizationService;
    }

    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _                                       => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message }                       => BotOnMessageReceived(message, cancellationToken),
            _                                              => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.From == null)
        {
            return;
        }

        var userData = await _userDataService.GetOrCreateUserAsync(message.From.Id, cancellationToken);

        if (string.IsNullOrWhiteSpace(userData.RefreshToken))
        {
            await ProcessNewUserAsync(message, userData, cancellationToken);
        }
        else
        {
            await ProcessExistingUserAsync(message, userData, cancellationToken);
        }
    }

    private async Task ProcessNewUserAsync(Message message, UserData userData, CancellationToken cancellationToken)
    {
        var url = await _authorizationService.CreateStateAndRedirectAsync(message.From!.Id, cancellationToken);
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Hey {message.From!.Id}, I don't know you. Go here please: {url}",
            cancellationToken: cancellationToken);
    }

    private async Task ProcessExistingUserAsync(Message message, UserData userData, CancellationToken cancellationToken)
    {
        var accessToken =
            await _authorizationService.GetTokenForRefreshTokenAsync(userData.RefreshToken!, cancellationToken);
        using var dropboxClient = new DropboxClient(accessToken);
        var currentAccount = await dropboxClient.Users.GetCurrentAccountAsync();
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Oh hi, I know you, your name is {currentAccount.Name.DisplayName}!",
            cancellationToken: cancellationToken);

        var files = await dropboxClient.Files.SearchV2Async(new(
            query: "kmy",
            options: new(
                filenameOnly: true,
                fileExtensions: new[] { "kmy" })));
        var fileNames = files
            .Matches
            .Select(m => (m.Metadata.AsMetadata.Value as FileMetadata)?.Name)
            .Where(fn => !string.IsNullOrWhiteSpace(fn))
            .Select(fn => fn!);
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Found following files:\n" + string.Join('\n', fileNames),
            cancellationToken: cancellationToken);
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}