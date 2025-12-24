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

    // ApiExplorer GroupNameFormat is configured as "'v'VVV" in AddApiVersion() -> "v1", "v2", ...
    var openApiDocuments = builder.Configuration.GetSection("OpenApi:Documents").Get<string[]>() ?? ["v1", "v2"];

    builder.Host.UseSerilog(Log.Logger);

    builder.Services.AddApiVersion();

    builder.Services.AddApplication(builder.Configuration);

    builder.Services.AddControllers();

    builder.Services.AddJwtAuthentication(builder.Configuration);

    builder.Services.AddOpenApi(builder.Configuration, openApiDocuments);

    var app = builder.Build();

    app.MapOpenApi("/openapi/{documentName}.json");

    app.MapScalarApiReference(o =>
    {
        o.WithTitle("My API Docs");
        o.WithTheme(ScalarTheme.Solarized);
        o.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        o.AddDocuments(openApiDocuments);
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