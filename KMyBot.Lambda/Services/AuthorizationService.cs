using System.Security.Cryptography;
using System.Text.Json;
using KMyBot.Lambda.Models;
using Microsoft.Extensions.Options;

namespace KMyBot.Lambda.Services;

public class AuthorizationService
{
    private readonly AuthConfiguration _authConfiguration;
    private readonly UserDataService _userDataService;
    private readonly HttpClient _httpClient;

    public AuthorizationService(IOptions<AuthConfiguration> authConfiguration, UserDataService userDataService)
    {
        _authConfiguration = authConfiguration.Value;
        _userDataService = userDataService;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.dropbox.com/"),
        };
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

    public async Task<string> GetRefreshTokenAsync(string code, CancellationToken ct)
    {
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["client_id"] = _authConfiguration.ClientId,
            ["client_secret"] = _authConfiguration.ClientSecret,
            ["redirect_uri"] = _authConfiguration.RedirectUri,
        });

        var response = await _httpClient.PostAsync("/oauth2/token", request, ct);
        var result = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(result);
        }

        var parsed = JsonSerializer.Deserialize<IDictionary<string, object>>(result);
        return parsed?["refresh_token"]?.ToString() ?? throw new Exception("Why is it null?");
    }

    public async Task<string> GetTokenForRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["client_id"] = _authConfiguration.ClientId,
            ["client_secret"] = _authConfiguration.ClientSecret,
        });

        var response = await _httpClient.PostAsync("/oauth2/token", request, ct);
        var result = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine("{0}", result);
            throw new Exception(result);
        }

        var parsed = JsonSerializer.Deserialize<IDictionary<string, object>>(result);
        return parsed?["access_token"]?.ToString() ?? throw new Exception("Why is it null?");
    }
}