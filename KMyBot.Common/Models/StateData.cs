using Amazon.DynamoDBv2.DataModel;

namespace KMyBot.Common.Models;

[DynamoDBTable("kMyTableStates")]
public class StateData
{
    [DynamoDBHashKey]
    public string State { get; set; } = default!;

    [DynamoDBProperty]
    public long Id { get; set; } = default!;
}