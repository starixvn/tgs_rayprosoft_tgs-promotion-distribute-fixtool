using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBucket.Clients.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true)
    .AddEnvironmentVariables();

builder.Services
    .AddFixToolRepository((sp, dbOptions) 
	    => _ = dbOptions.UseNpgsql(builder.Configuration.GetConnectionString("Tgs")))
    .AddFixToolCore()
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("FixTool:CashBaseUrl")!;
    })
    .AddPlayerServiceClient((options, _) =>
        options.BaseUrl = builder.Configuration.GetValue<Uri>("FixTool:PlayerServiceBaseUrl")!)
    .AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
        .ConfigureHttpClient(httpClient =>
            httpClient.BaseAddress = builder.Configuration.GetValue<Uri>("FixTool:PlayerBucketBaseUrl")));

builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(config =>
        config.ConfigQueueOptions((options, _) =>
        {
            options.Url = builder.Configuration.GetConnectionString("Nats")!;
        }))
    .AddNatsGlobPatternExchange("*");

builder.Services.AddControllers();

builder.Services
    .AddSwaggerGen();

var app = builder.Build();

app.MapControllers();
app.MapSwagger();
app.UseSwaggerUI();

app.Run();
