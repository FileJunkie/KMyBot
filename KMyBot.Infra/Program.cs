using System;
using System.Collections.Generic;
using System.Text.Json;
using Pulumi;
using Pulumi.Aws.Lambda.Inputs;
using Aws = Pulumi.Aws;
using AwsApiGateway = Pulumi.AwsApiGateway;

return await Deployment.RunAsync(() => 
{
    var role = new Aws.Iam.Role("role", new()
    {
        AssumeRolePolicy = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["Version"] = "2012-10-17",
            ["Statement"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["Action"] = "sts:AssumeRole",
                    ["Effect"] = "Allow",
                    ["Principal"] = new Dictionary<string, object?>
                    {
                        ["Service"] = "lambda.amazonaws.com",
                    },
                },
            },
        }),
        ManagedPolicyArns = new[]
        {
            "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole",
        },
    });

    var fn = new Aws.Lambda.Function("fn", new()
    {
        Runtime = Aws.Lambda.Runtime.Dotnet6,
        Handler = "KMyBot.Lambda::KMyBot.Lambda.LambdaEntryPoint::FunctionHandlerAsync",
        Role = role.Arn,
        Code = new FileArchive("../output/KMyBot.Lambda.zip"),
        Environment = new FunctionEnvironmentArgs
        {
            Variables =
            {
                ["BOT_TOKEN"] = Output.CreateSecret(Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new Exception("BOT_TOKEN not defined")),
                ["BOT_SECRET_TOKEN"] = Output.CreateSecret(Environment.GetEnvironmentVariable("BOT_SECRET_TOKEN") ?? throw new Exception("BOT_SECRET_TOKEN not defined")),
            }
        },
        Timeout = 5 * 60,
    });

    var api = new AwsApiGateway.RestAPI("api", new()
    {
        Routes =
        {
            new AwsApiGateway.Inputs.RouteArgs
            {
                Path = "/",
                Method = AwsApiGateway.Method.GET,
                EventHandler = fn,
            },
            new AwsApiGateway.Inputs.RouteArgs
            {
                Path = "/api/bot",
                Method = AwsApiGateway.Method.POST,
                EventHandler = fn,
            },
        },
    });

    return new Dictionary<string, object?>
    {
        ["url"] = api.Url,
    };
});

