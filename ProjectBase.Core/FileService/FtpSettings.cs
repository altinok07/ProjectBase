namespace ProjectBase.Core.FileService
{
    public class FtpSettings
    {
        /// <summary>
        /// IIS FTP host. Örnek: "cdn.lokmanecza.com" veya "ftp://cdn.lokmanecza.com"
        /// </summary>
        /// <summary>
        /// IIS FTP host. Aynı host dönen Path için https:// ile kullanılır. Örnek: "cdn.lokmanecza.com"
        /// </summary>
        public string FtpAddress { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
