using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Pulumi;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Serilog;

class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Deploy);

    [Solution] readonly Solution Solution;
    Project LambdaProject => Solution.Projects.Single(p => p.Name == "KMyBot.Lambda");
    Project InfraProject => Solution.Projects.Single(p => p.Name == "KMyBot.Infra");
    [NuGetPackage("Amazon.Lambda.Tools", "dotnet-lambda.dll")] readonly Tool AwsLambda;

    AbsolutePath OutputDirectory => RootDirectory / "output";

    [Parameter] string BotToken { get; set; } = null!;
    [Parameter] string BotSecretToken { get; set; } = null!;

    Target Clean => _ => _
        .Executes(() =>
        {
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            AwsLambda(
                arguments: $"package -c Release -o {OutputDirectory / "KMyBot.Lambda.zip"}",
                workingDirectory: LambdaProject.Directory);
        });

    Target Deploy => _ => _
        .DependsOn(Compile)
        .Executes(() =>  
        {  
            PulumiTasks.PulumiUp(_ => _
                .SetCwd(InfraProject.Directory)  
                .SetStack("dev")  
                .EnableSkipPreview());
        });

    Target UpdateEndpoint => _ => _
        .TriggeredBy(Deploy)
        .Requires(() => BotToken)
        .Requires(() => BotSecretToken)
        .Executes(async () =>
        {
            var mainUri = new Uri(GetPulumiOutput("Url"));
            Log.Information("Setting main URI is {MainUri}", mainUri);
            var webhookAddress = new Uri(mainUri, "./api/bot");
            Log.Information("Setting webhook URI to {WebhookUri}", webhookAddress);
            var options = new TelegramBotClientOptions(BotToken);
            var httpClient = new HttpClient();
            var botClient = new TelegramBotClient(options, httpClient);
            await botClient.SetWebhookAsync(
                url: webhookAddress.ToString(),
                allowedUpdates: Array.Empty<UpdateType>(),
                secretToken: BotSecretToken,
                cancellationToken: CancellationToken.None);
        });

    string GetPulumiOutput(string outputName)  
    {  
        return PulumiTasks.PulumiStackOutput(_ => _  
                .SetCwd(InfraProject.Directory)
                .SetPropertyName(outputName)
                .EnableShowSecrets()
                .DisableProcessLogOutput())
            .StdToText();
    }
}
