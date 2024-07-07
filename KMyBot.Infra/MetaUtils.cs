using System;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using Pulumi.Aws.DynamoDB.Inputs;

namespace KMyBot.Infra;

public static class MetaUtils
{
    private static readonly Type[] NumericTypes =
    [
        typeof(int),
        typeof(long),
        typeof(double),
    ];

    public static Pulumi.Aws.DynamoDB.Table CreateTable<T>()
    {
        var name = typeof(T).GetCustomAttribute<DynamoDBTableAttribute>()?.TableName ??
                   throw new Exception("Forgot to name your table");

        var hashProperty = typeof(T).GetProperties().Single(p =>
            p.GetCustomAttribute<DynamoDBHashKeyAttribute>() != null);
        var hashKey = hashProperty.Name ?? throw new Exception("Wait, what?");
        string propertyType;
        if (hashProperty.PropertyType == typeof(string))
        {
            propertyType = "S";
        }
        else if (NumericTypes.Contains(hashProperty.PropertyType))
        {
            propertyType = "N";
        }
        else
        {
            throw new Exception("What type is it?");
        }

        return new(name, new()
        {
            Name = name,
            ReadCapacity = 1,
            WriteCapacity = 1,
            HashKey = hashKey ?? throw new Exception("What's the hash key?"),
            Attributes = new TableAttributeArgs
            {
                Name = hashKey,
                Type = propertyType,
            },
        });
    }
}