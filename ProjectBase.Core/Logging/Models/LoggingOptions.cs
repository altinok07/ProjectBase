namespace ProjectBase.Core.Logging.Models;

public class LoggingOptions
{
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableResponseLogging { get; set; } = true;
    public bool MaskSensitiveData { get; set; } = true;
    public int MaxBodyLength { get; set; } = 64 * 1024;
}