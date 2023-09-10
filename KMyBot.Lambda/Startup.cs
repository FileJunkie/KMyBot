using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using KMyBot.Lambda.Controllers;
using KMyBot.Lambda.Models;
using KMyBot.Lambda.Services;
using Telegram.Bot;

namespace KMyBot.Lambda;

public class Startup
{
    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        Configuration = builder.BuildAndReplacePlaceholders();
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        var botConfigurationSection = Configuration.GetSection(BotConfiguration.Configuration);
        services.Configure<BotConfiguration>(botConfigurationSection);
        var authConfigurationSection = Configuration.GetSection(AuthConfiguration.Configuration);
        services.Configure<AuthConfiguration>(authConfigurationSection);

        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                var botConfig = sp.GetConfiguration<BotConfiguration>();
                TelegramBotClientOptions options = new(botConfig.BotToken);
                return new TelegramBotClient(options, httpClient);
            });

        services.AddScoped<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddScoped<DynamoDBContext>(sp =>
            new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));
        services.AddScoped<UserDataService>();
        services.AddScoped<AuthorizationService>();

        services.AddScoped<UpdateHandlers>();
        services
            .AddControllers()
            .AddNewtonsoftJson();

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}