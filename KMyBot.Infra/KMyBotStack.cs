using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KMyBot.Common.Models;
using Pulumi;
using Pulumi.Aws.ApiGateway;
using Pulumi.Aws.ApiGateway.Inputs;
using Pulumi.Aws.DynamoDB;
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
        var tables = new[]
        {
            MetaUtils.CreateTable<StateData>(),
            MetaUtils.CreateTable<UserData>(),
        };
        var role = CreateRole(tables);
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
        return new ("dnsRecord", new()
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

    private Role CreateRole(Table[] tables)
    {
        var tableResourceIds= Output.All(tables.Select(t => t.Arn));
        var inlinePolicy = tableResourceIds.Apply(ta => JsonSerializer.Serialize(new Dictionary<string, object?>
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
        var envVars = new[] { "BOT_TOKEN", "BOT_SECRET_TOKEN", "REDIRECT_URI", "DROPBOX_KEY", "DROPBOX_SECRET" };
        InputMap<string> variables = new();
        foreach (var envVar in envVars)
        {
            variables[envVar] = Output.CreateSecret(Environment.GetEnvironmentVariable(envVar) ??
                                                    throw new Exception($"{envVar} not defined"));
        }

        return new Function("fn", new()
        {
            Name = "kMyBot",
            Runtime = Runtime.Dotnet6,
            Handler = "KMyBot.Lambda::KMyBot.Lambda.LambdaEntryPoint::FunctionHandlerAsync",
            Role = role.Arn,
            Code = new FileArchive("../output/KMyBot.Lambda.zip"),
            Environment = new FunctionEnvironmentArgs
            {
                Variables = variables,
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