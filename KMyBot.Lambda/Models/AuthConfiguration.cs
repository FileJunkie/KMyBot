namespace KMyBot.Lambda.Models;

public class AuthConfiguration
{
    public static readonly string Configuration = nameof(AuthConfiguration);

    public string RedirectUri { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}