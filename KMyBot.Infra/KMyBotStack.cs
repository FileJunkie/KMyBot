using System;
using System.Collections.Generic;
using System.Text.Json;
using Pulumi;
using Pulumi.Aws.ApiGateway;
using Pulumi.Aws.ApiGateway.Inputs;
using Pulumi.Aws.DynamoDB;
using Pulumi.Aws.DynamoDB.Inputs;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Iam.Inputs;
using Pulumi.Aws.Lambda;
using Pulumi.Aws.Lambda.Inputs;
using Aws = Pulumi.Aws;
using AwsApiGateway = Pulumi.AwsApiGateway;

namespace KMyBot.Infra;

public class KMyBotStack : Stack
{
    private readonly Config _config = new();
    public KMyBotStack()
    {
        var domain = CreateCustomDomain();
        _ = CreateDnsRecord(domain);
        var table = CreateDynamoDbTable();
        var role = CreateRole(table.Arn);
        var function = CreateFunction(role);
        var api = CreateApi(function);
        var apiMapping = CreateApiMapping(domain, api);
        Url = Output.Create($"https://{_config.DomainName}/");
    }

    [Output] public Output<string> Url { get; set; }

    private Aws.ApiGateway.DomainName CreateCustomDomain()
    {
        return new ("domainName", new()
        {
            CertificateArn = _config.DomainCertificateArn,
            Domain = _config.DomainName,
            EndpointConfiguration = new DomainNameEndpointConfigurationArgs
            {
                Types = "EDGE",
            },
        });
    }

    private Aws.Route53.Record CreateDnsRecord(Aws.ApiGateway.DomainName domain)
    {
        return new ("exampleRecord", new()
        {
            Name = domain.Domain,
            Type = "A",
            ZoneId = _config.DomainZoneId,
            Aliases = new[]
            {
                new Aws.Route53.Inputs.RecordAliasArgs
                {
                    EvaluateTargetHealth = true,
                    Name = domain.CloudfrontDomainName,
                    ZoneId = domain.CloudfrontZoneId,
                },
            },
        });
    }

    private Role CreateRole(Output<string> tableArn)
    {
        var inlinePolicy = tableArn.Apply(ta => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["Version"] = "2012-10-17",
            ["Statement"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["Action"] = new[]
                    {
                        "dynamodb:*",
                    },
                    ["Effect"] = "Allow",
                    ["Resource"] = ta,
                },
             },
        }));

        return new ("role", new()
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
            InlinePolicies = new RoleInlinePolicyArgs
            {
                Name = "dynamodb-inline-policy",
                Policy = inlinePolicy,
            },
            ManagedPolicyArns = new[]
            {
                "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole",
            },
        });
    }

    private Function CreateFunction(Role role)
    {
        return new Function("fn", new()
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
    }

    private AwsApiGateway.RestAPI CreateApi(Function fn)
    {
        return new ("api", new()
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
    }

    private Table CreateDynamoDbTable()
    {
        return new ("kMyTable", new()
        {
            Name = "kMyTable",
            ReadCapacity = 1,
            WriteCapacity = 1,
            HashKey = "Id",
            Attributes = new TableAttributeArgs
            {
                Name = "Id",
                Type = "N",
            }
        });
    }

    private BasePathMapping CreateApiMapping(DomainName domain, AwsApiGateway.RestAPI api)
    {
        return new("pathMapping", new()
        {
            RestApi = api.Api.Apply(a => a.Id),
            StageName = api.Stage.Apply(s => s.StageName),
            DomainName = domain.Domain,
        });
    }
}