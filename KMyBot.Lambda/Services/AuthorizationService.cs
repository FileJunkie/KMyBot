using System.Security.Cryptography;
using KMyBot.Lambda.Models;
using Microsoft.Extensions.Options;

namespace KMyBot.Lambda.Services;

public class AuthorizationService
{
    private readonly AuthConfiguration _authConfiguration;
    private readonly UserDataService _userDataService;

    public AuthorizationService(IOptions<AuthConfiguration> authConfiguration, UserDataService userDataService)
    {
        _authConfiguration = authConfiguration.Value;
        _userDataService = userDataService;
    }

    public async Task<string> CreateStateAndRedirectAsync(long userId, CancellationToken cancellationToken)
    {
        var state = RandomString(24);
        await _userDataService.UpsertUsersStateAsync(userId, state, cancellationToken);

        return $"https://www.dropbox.com/oauth2/authorize?" +
               $"client_id={_authConfiguration.ClientId}" +
               $"&response_type=code" +
               $"&token_access_type=offline" +
               $"&state={state}" +
               $"&redirect_uri={_authConfiguration.RedirectUri}";
    }

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
    }
}