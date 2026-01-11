using Herrmann.MesseApp.Server.Services;
using Microsoft.Net.Http.Headers;
using Serilog;

// Configure Serilog - initially with bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starte Messe Server...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    
    builder.Services.AddSignalR();

    
    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services
        .AddSingleton<TradeEventsService>()
        .AddSingleton<EventInventoriesService>()
        .AddSingleton<ArticlesService>()
        .AddSingleton<BarcodeScannerService>()
        .AddHostedService<BarcodeScannerBackgroundService>()
        ;
    
// Add Swagger services
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Messe Server API",
            Version = "v1",
            Description = "API für die Messe-Anwendung"
        });
    });

    var app = builder.Build();

// Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseDefaultFiles();
    var staticFilesOptions = new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            if (string.Equals(ctx.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-store";
            }
        }
    };
    app.UseStaticFiles(staticFilesOptions);

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Messe Server API v1");
            options.RoutePrefix = "swagger"; // Swagger UI wird unter /swagger verfügbar sein
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.MapHub<NotificationHub>("/hubs/notification");
    
    Log.Information("Messe Server gestartet");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Messe Server wurde unerwartet beendet");
    throw;
}
finally
{
    Log.Information("Messe Server wird beendet...");
    await Log.CloseAndFlushAsync();
}