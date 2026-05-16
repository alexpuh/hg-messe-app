using Herrmann.MesseApp.Server.Controllers;
using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Filters;
using Herrmann.MesseApp.Server.Services;
using Microsoft.EntityFrameworkCore;
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
    
    builder.Services
        .AddSignalR();

    // Configure SQLite Database
    // Supports override via ConnectionStrings__DefaultConnection env var (used for E2E test isolation)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. " +
            "Add it to appsettings.json or set ConnectionStrings__DefaultConnection.");
    builder.Services
        .AddDbContext<MesseAppDbContext>(options => options.UseSqlite(connectionString));

    
    // Add services to the container.

    var mvcBuilder = builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // DebugController is excluded from non-Development environments entirely — its routes
    // are never registered, so no filter, misconfiguration, or routing quirk can expose it.
    // The [DevelopmentOnly] filter on the controller provides a second layer in Development itself.
    if (!builder.Environment.IsDevelopment())
    {
        mvcBuilder.ConfigureApplicationPartManager(m =>
            m.FeatureProviders.Add(new ExcludeControllersFeatureProvider(typeof(DebugController))));
    }
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services
        .AddScoped<DispatchSheetService>()
        .AddScoped<ArticlesService>()
        .AddScoped<ScanSessionService>()
        .AddScoped<SignalNotificationService>()
        .AddScoped<ScanSessionExcelExportService>()
        .AddSingleton<BarcodeScannerService>()
        ;

    // Allow E2E tests (and dev machines without a scanner) to opt out of the background
    // service that attempts to open a COM port. The BarcodeScannerService itself is always
    // registered so BarcodeScannerController.GetStatus() continues to work.
    if (!builder.Configuration.GetValue<bool>("BarcodeScanner:DisableBackgroundService"))
    {
        builder.Services.AddHostedService<BarcodeScannerBackgroundService>();
    }
    
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

    // Initialize database
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MesseAppDbContext>();
        Log.Information("Initialisiere Datenbank...");
        await dbContext.Database.EnsureCreatedAsync();
        Log.Information("Datenbank initialisiert");
    }

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