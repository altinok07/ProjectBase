namespace ProjectBase.Core.Logging;

internal static class LogFields
{
    public const string CorrelationId = "correlation.id";
    public const string RequestName = "request.name";
    public const string RequestBody = "request.body";
    public const string ResponseBody = "response.body";
    public const string ElapsedMs = "elapsed.ms";
    public const string MessageSource = "message.source";

    public const string HttpMethod = "http.method";
    public const string HttpPath = "http.path";
    public const string HttpQuery = "http.query";
    public const string HttpStatusCode = "http.status.code";
}
