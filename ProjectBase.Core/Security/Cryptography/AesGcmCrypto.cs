using System.Security.Cryptography;
using System.Text;

namespace ProjectBase.Core.Security.Cryptography;

public static class AesGcmCrypto
{
    public const int KeySizeBytes = 32;   // 256-bit
    private const int NonceSize = 12;     // GCM için tavsiye edilen nonce
    private const int TagSize = 16;     // 128-bit auth tag
    private const byte Version = 0x01;   // payload versiyonu

    // Global anahtar (opsiyonel): InitializeFromBase64 ile set edilir
    private static byte[]? _key;

    /// <summary>Uygulama başlarken config'ten çağır (örn. builder.Configuration["Crypto:Key"]).</summary>
    public static void InitializeFromBase64(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new ArgumentNullException(nameof(base64Key));

        var key = Convert.FromBase64String(base64Key);
        if (key.Length != KeySizeBytes)
            throw new ArgumentException($"Key must be {KeySizeBytes} bytes (Base64).");

        // Thread-safe atama
        Volatile.Write(ref _key, key);
    }

    /// <summary>Yeni bir 256-bit key üretir (Base64). Bunu config/KeyVault'ta saklayın.</summary>
    public static string GenerateKeyBase64()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySizeBytes));

    // ========== Global anahtarla kullanım ==========

    public static string Encrypt(string plainText)
    {
        var key = Volatile.Read(ref _key)
                  ?? throw new InvalidOperationException("AesGcmCrypto is not initialized. Call InitializeFromBase64(...) or use the overload that takes a key.");
        return Encrypt(plainText, key);
    }

    public static string Decrypt(string base64Payload)
    {
        var key = Volatile.Read(ref _key)
                  ?? throw new InvalidOperationException("AesGcmCrypto is not initialized. Call InitializeFromBase64(...) or use the overload that takes a key.");
        return Decrypt(base64Payload, key);
    }

    public static bool TryDecrypt(string base64Payload, out string? plainText)
    {
        try { plainText = Decrypt(base64Payload); return true; }
        catch { plainText = null; return false; }
    }

    // ========== Çağrı bazlı anahtar ile kullanım ==========

    public static string Encrypt(string plainText, ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySizeBytes) throw new ArgumentException($"Key must be {KeySizeBytes} bytes.", nameof(key));

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var pt = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
        var ct = new byte[pt.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key.ToArray(), TagSize))  // ⬅️ yeni kurucu
        {
            aes.Encrypt(nonce, pt, ct, tag);
        }

        // Payload: [V][nonce][tag][ciphertext]  -> Base64
        var payload = new byte[1 + NonceSize + TagSize + ct.Length];
        payload[0] = Version;
        Buffer.BlockCopy(nonce, 0, payload, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(ct, 0, payload, 1 + NonceSize + TagSize, ct.Length);

        return Convert.ToBase64String(payload);
    }

    public static string Decrypt(string base64Payload, ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySizeBytes) throw new ArgumentException($"Key must be {KeySizeBytes} bytes.", nameof(key));
        if (string.IsNullOrWhiteSpace(base64Payload)) throw new ArgumentNullException(nameof(base64Payload));

        var payload = Convert.FromBase64String(base64Payload);
        if (payload.Length < 1 + NonceSize + TagSize)
            throw new FormatException("Ciphertext payload too short.");

        var ver = payload[0];
        if (ver != Version)
            throw new NotSupportedException($"Unsupported payload version: {ver}");

        var nonce = new ReadOnlySpan<byte>(payload, 1, NonceSize).ToArray();
        var tag = new ReadOnlySpan<byte>(payload, 1 + NonceSize, TagSize).ToArray();
        var ct = new ReadOnlySpan<byte>(payload, 1 + NonceSize + TagSize, payload.Length - (1 + NonceSize + TagSize)).ToArray();
        var pt = new byte[ct.Length];

        using (var aes = new AesGcm(key.ToArray(), TagSize))  // ⬅️ yeni kurucu
        {
            aes.Decrypt(nonce, ct, tag, pt);
        }

        return Encoding.UTF8.GetString(pt);
    }
}