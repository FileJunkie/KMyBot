using Amazon.DynamoDBv2.DataModel;

namespace KMyBot.Common.Models;

[DynamoDBTable("kMyTableStates")]
public class StateData
{
    [DynamoDBHashKey]
    public required string State { get; set; }

    [DynamoDBProperty]
    public required long Id { get; set; }
}