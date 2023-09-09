using Amazon.DynamoDBv2.DataModel;

namespace KMyBot.Common.Models;

[DynamoDBTable("kMyTableUsers")]
public class UserData
{
    [DynamoDBHashKey]
    public long Id { get; set; }

    [DynamoDBProperty]
    public string? RefreshToken { get; set; }

    [DynamoDBProperty]
    public string? SelectedFile { get; set; }
}