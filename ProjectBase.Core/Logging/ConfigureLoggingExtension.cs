using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;
using System.Reflection;

namespace ProjectBase.Core.Extensions;

public static class ConfigureLoggingExtension
{
    public static LoggerConfiguration ConfigureLogging(this LoggerConfiguration loggerConfig) => loggerConfig
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Filter.ByExcluding(Matching.FromSource("Microsoft"))
        .Filter.ByExcluding(Matching.FromSource("System"))
        .WriteTo.Console(theme: SystemConsoleTheme.Literate)
        //.WriteTo.Elasticsearch(GetElasticsearchSinkOptions())
        .WriteTo.Seq(serverUrl: GetSeqServerUrl())
        .Enrich.FromLogContext();

    #region ElsticSearch Configuration
    private static ElasticsearchSinkOptions GetElasticsearchSinkOptions() => new(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        CustomFormatter = new Serilog.Formatting.Elasticsearch.ElasticsearchJsonFormatter(),
        IndexFormat = $"{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name!.ToLower().Replace(".", "-")}-{DateTime.Now:yyyy-MM-dd}"
    };
    #endregion

    #region Seq Configuration
    private static string GetSeqServerUrl() => "http://localhost:5341";
    private static ITextFormatter GetFormatProvider() => new RenderedCompactJsonFormatter();
    #endregion
}
