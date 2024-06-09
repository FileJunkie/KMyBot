using Dropbox.Api;
using Dropbox.Api.Files;
using KMyBot.Common.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KMyBot.Lambda.Services;

public class UpdateHandlers(
    ITelegramBotClient botClient,
    ILogger<UpdateHandlers> logger,
    UserDataService userDataService,
    AuthorizationService authorizationService)
{
    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _                                       => exception.ToString()
        };

        logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);
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
        logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.From == null)
        {
            return;
        }

        var userData = await userDataService.GetOrCreateUserAsync(message.From.Id, cancellationToken);

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
        var url = await authorizationService.CreateStateAndRedirectAsync(message.From!.Id, cancellationToken);
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Hey {message.From!.Id}, I don't know you. Go here please: {url}",
            cancellationToken: cancellationToken);
    }

    private async Task ProcessExistingUserAsync(Message message, UserData userData, CancellationToken cancellationToken)
    {
        var accessToken =
            await authorizationService.GetTokenForRefreshTokenAsync(userData.RefreshToken!, cancellationToken);
        using var dropboxClient = new DropboxClient(accessToken);
        var currentAccount = await dropboxClient.Users.GetCurrentAccountAsync();
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Oh hi, I know you, your name is {currentAccount.Name.DisplayName}!",
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(userData.SelectedFile))
        {
            var files = await dropboxClient.Files.SearchV2Async(new(
                query: "kmy",
                options: new(
                    filenameOnly: true,
                    fileExtensions: new[] { "kmy" })));
            var fileNames = files
                .Matches
                .Select(m => (m.Metadata.AsMetadata.Value as FileMetadata)?.Name)
                .Where(fn => !string.IsNullOrWhiteSpace(fn))
                .Select(fn => fn!)
                .ToList();
            if (!string.IsNullOrWhiteSpace(message.Text) && fileNames.Contains(message.Text))
            {
                userData.SelectedFile = message.Text;
                await userDataService.UpdateUserAsync(userData, cancellationToken);
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "File selection saved",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Found following files",
                    replyMarkup: new ReplyKeyboardMarkup(fileNames.Select(fn => new KeyboardButton(fn))),
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"You chose file {userData.SelectedFile}",
                cancellationToken: cancellationToken);
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}