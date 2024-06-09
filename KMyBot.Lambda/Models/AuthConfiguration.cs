namespace KMyBot.Lambda.Models;

public class AuthConfiguration
{
    public static readonly string Configuration = nameof(AuthConfiguration);

    public required string RedirectUri { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}