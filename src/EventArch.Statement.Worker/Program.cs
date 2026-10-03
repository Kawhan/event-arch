using EventArch.Statement.Worker;
using EventArch.Statement.Worker.Persistence;
using Serilog;

// Bootstrap logger: captures errors that happen before the configuration is loaded.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddStatementWorker(builder.Configuration);

    var host = builder.Build();

    if (builder.Environment.IsDevelopment())
    {
        await host.Services.ApplyMigrationsAsync();
    }

    await host.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Statement worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
