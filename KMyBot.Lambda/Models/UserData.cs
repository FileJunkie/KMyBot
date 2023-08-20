using Amazon.DynamoDBv2.DataModel;

namespace KMyBot.Lambda.Models;

[DynamoDBTable("kMyTable")]
public class UserData
{
    [DynamoDBHashKey]
    public long Id { get; set; } = default!;
}