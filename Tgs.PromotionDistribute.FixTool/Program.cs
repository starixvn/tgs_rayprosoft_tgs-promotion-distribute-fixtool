using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.PlayerBucket.Clients.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true)
    .AddEnvironmentVariables();

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;

		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack();
		});
	});

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
	        options.Url = builder.Configuration.GetConnectionString("Nats")!))
    .AddNatsGlobPatternExchange("*");

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers();

builder.Services
	.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
		sp => new ConfigureNamedOptions<OtlpExporterOptions>(
			Options.DefaultName,
			exporterOptions =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				var nodeIp = config.GetValue<string>("NODE_IP");

				if (!string.IsNullOrWhiteSpace(nodeIp))
					exporterOptions.Endpoint = new Uri($"http://{nodeIp}:4317");

				config.GetSection("Opentelemetry:Tracing").Bind(exporterOptions);
			}))
	.AddProfileViewer()
	.Configure<AspNetCoreInstrumentationOptions>(
		options => options.Filter = httpContext
			=> !httpContext.Request.Path.HasValue
			   || !httpContext.Request.Path.StartsWithSegments("/swagger")
			   && !httpContext.Request.Path.StartsWithSegments("/healthz")
			   && !httpContext.Request.Path.StartsWithSegments("/profiler/")
			   && !httpContext.Request.Path.StartsWithSegments("/metrics")
			   && !new[] { "html", "ico", "png", "jpg" }.Any(
				   ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
	.AddOpenTelemetry()
	.WithTracing(traceBuilder => traceBuilder
		.SetResourceBuilder(ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddNpgsql()
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterBuilder => meterBuilder
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddRuntimeInstrumentation()
		.AddPrometheusExporter())
	.StartWithHost();
	
builder.Services
    .AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapSwagger();
app.UseSwaggerUI();
app.MapProfileViewer();

app.Run();
