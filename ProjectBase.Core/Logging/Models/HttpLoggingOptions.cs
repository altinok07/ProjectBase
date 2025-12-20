using Serilog.Events;
namespace ProjectBase.Core.Logging.Models;

public sealed class HttpLoggingOptions : LoggingOptions
{
    public const string DefaultCorrelationHeader = "X-Correlation-Id";

    public string CorrelationHeaderName { get; set; } = DefaultCorrelationHeader;

    public string[] ExcludePaths { get; set; } =
        [
        // Sağlık ve Metrikler
        "/health",
        "/healthz",
        "/ready",
        "/readyz",
        "/live",
        "/metrics",
        "/ping",

        // API Dokümantasyonu
        "/swagger",
        "/swagger-ui",
        "/swagger/v1/swagger.json",
        "/api-docs",

        // Kimlik Doğrulama ve Token Endpointleri
        "/auth/token",
        "/auth/login",
        "/auth/logout",
        "/oauth/",
        "/connect/token",
        "/connect/authorize",

        // Statik İçerik
        "/images",
        "/css",
        "/js",
        "/static",
        "/fonts",
        "/media",
        "/favicon.ico",

        // Büyük Dosya Yükleme ve İndirme
        "/api/upload",
        "/api/download",
        "/api/files",
        "/exports/",

        // Webhook ve Test Endpointleri
        "/webhooks/",
        "/debug",
        "/test",
        "/sandbox"
        ];

    public bool TruncateRequestBody { get; set; } = true;
    public int MaxRequestBodyBytes { get; set; } = 64 * 1024;
    public int MaxResponseBodyBytes { get; set; } = 64 * 1024;

    public string MaskWith { get; set; } = "****";
    public string[] SensitiveFields { get; set; } = ["password", "token", "accessToken", "authorization", "ssn", "iban", "creditCard", "cardNumber", "tcKimlik", "email", "phone"];

    public string ResponseLogLevel { get; set; } = LogEventLevel.Information.ToString();
    public string ErrorLogLevel { get; set; } = LogEventLevel.Error.ToString();

    public SensitiveFieldMatchMode SensitiveFieldMatchMode { get; set; } = SensitiveFieldMatchMode.Contains;
}

public enum SensitiveFieldMatchMode { Contains, Equals, StartsWith }
