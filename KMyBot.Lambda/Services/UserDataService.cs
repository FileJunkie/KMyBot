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
}