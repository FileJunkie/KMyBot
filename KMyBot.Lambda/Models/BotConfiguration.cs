namespace KMyBot.Lambda.Models;

public class BotConfiguration
{
    public static readonly string Configuration = nameof(BotConfiguration);

    public string BotToken { get; init; } = default!;
    public string SecretToken { get; init; } = default!;
}