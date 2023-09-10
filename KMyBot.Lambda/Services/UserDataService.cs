using Amazon.DynamoDBv2.DataModel;
using KMyBot.Common.Models;

namespace KMyBot.Lambda.Services;

public class UserDataService
{
    private readonly ILogger<UserDataService> _logger;
    private readonly DynamoDBContext _dynamodbContext;

    public UserDataService(ILogger<UserDataService> logger, DynamoDBContext dynamodbContext)
    {
        _logger = logger;
        _dynamodbContext = dynamodbContext;
    }

    public async Task<UserData> GetOrCreateUserAsync(long id, CancellationToken ct)
    {
        var userData = await _dynamodbContext.LoadAsync<UserData>(id, ct);

        if (userData == null)
        {
            _logger.LogInformation("No user with ID {UserId} yet, creating", id);
            userData = new UserData { Id = id };
            await _dynamodbContext.SaveAsync(userData, ct);
        }
        else
        {
            _logger.LogInformation("User with ID {UserId} already exists, returning", id);
        }

        return userData;
    }

    public async Task UpsertUsersStateAsync(long userId, string state, CancellationToken cancellationToken)
    {
        await _dynamodbContext.SaveAsync(new StateData
            {
                Id = userId,
                State = state,
            },
            cancellationToken);
    }

    public async Task UpdateUserAsync(UserData userData, CancellationToken cancellationToken)
    {
        await _dynamodbContext.SaveAsync(userData, cancellationToken);
    }

    public async Task SaveRefreshTokenAsync(string state, string refreshToken, CancellationToken cancellationToken)
    {
        var stateData = await _dynamodbContext.LoadAsync<StateData>(state, cancellationToken);
        if (stateData == null)
        {
            throw new Exception("No user for this state");
        }

        await _dynamodbContext.SaveAsync(new UserData
        {
            Id = stateData.Id,
            RefreshToken = refreshToken,
        }, cancellationToken);
    }
}