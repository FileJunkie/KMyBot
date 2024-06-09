using Amazon.DynamoDBv2.DataModel;
using KMyBot.Common.Models;

namespace KMyBot.Lambda.Services;

public class UserDataService(ILogger<UserDataService> logger, DynamoDBContext dynamodbContext)
{
    public async Task<UserData> GetOrCreateUserAsync(long id, CancellationToken ct)
    {
        var userData = await dynamodbContext.LoadAsync<UserData>(id, ct);

        if (userData == null)
        {
            logger.LogInformation("No user with ID {UserId} yet, creating", id);
            userData = new UserData { Id = id };
            await dynamodbContext.SaveAsync(userData, ct);
        }
        else
        {
            logger.LogInformation("User with ID {UserId} already exists, returning", id);
        }

        return userData;
    }

    public async Task UpsertUsersStateAsync(long userId, string state, CancellationToken cancellationToken)
    {
        await dynamodbContext.SaveAsync(new StateData
            {
                Id = userId,
                State = state,
            },
            cancellationToken);
    }

    public async Task UpdateUserAsync(UserData userData, CancellationToken cancellationToken)
    {
        await dynamodbContext.SaveAsync(userData, cancellationToken);
    }

    public async Task SaveRefreshTokenAsync(string state, string refreshToken, CancellationToken cancellationToken)
    {
        var stateData = await dynamodbContext.LoadAsync<StateData>(state, cancellationToken);
        if (stateData == null)
        {
            throw new Exception("No user for this state");
        }

        await dynamodbContext.SaveAsync(new UserData
        {
            Id = stateData.Id,
            RefreshToken = refreshToken,
        }, cancellationToken);
    }
}