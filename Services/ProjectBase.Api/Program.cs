using ProjectBase.Application;
using ProjectBase.Core.Extensions;
using Scalar.AspNetCore;
using Serilog;

// Serilog yapılandırması (Host oluşturulmadan önce)
Log.Logger = new LoggerConfiguration()
    .ConfigureLogging()
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(Log.Logger);

    builder.Services.AddApiVersion();

    builder.Services.AddApplication(builder.Configuration);

    builder.Services.AddControllers();

    builder.Services.AddJwtAuthentication(builder.Configuration);

    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.MapOpenApi();

    app.MapScalarApiReference(o =>
    {
        o
            .WithTitle("My API Docs")
            .WithTheme(ScalarTheme.Solarized);
    });

    app.UseMiddlewares();

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}