using EventArch.Api.Errors;
using EventArch.Application;
using EventArch.Infrastructure;
using EventArch.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger: captures errors that happen before the configuration is loaded.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Sinks, levels and enrichers come from the "Serilog" section of appsettings.json.
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();   // UI at /scalar
        await app.Services.ApplyMigrationsAsync();
    }

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is how test hosts (WebApplicationFactory) stop Program after
    // capturing the built app; it is not a crash and must not be logged as one.
    Log.Fatal(exception, "API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
