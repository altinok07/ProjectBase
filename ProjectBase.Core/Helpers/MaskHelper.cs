using ProjectBase.Core.Logging.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ProjectBase.Core.Helpers;

internal static partial class MaskHelper
{
    #region Regexes

    private static readonly Regex PlainJsonKeyValueRegex = JsonStringFieldRegex();

    private static readonly Regex KeyEqualsValueRegex = QueryOrFormKeyValuePairRegex();

    [GeneratedRegex(@"(?i)(?:\""(?<k>[A-Za-z0-9_\-]+)\""\s*:\s*\"")(?<v>[^\""]{0,2000})\""", RegexOptions.Compiled, "tr-TR")]
    private static partial Regex JsonStringFieldRegex();

    [GeneratedRegex(@"(?i)(?<k>[A-Za-z0-9_\-]+)=(?<v>[^&\s]{0,2000})", RegexOptions.Compiled, "tr-TR")]
    private static partial Regex QueryOrFormKeyValuePairRegex();

    #endregion

    #region Masking

    public static string MaybeMask(string payload, HttpLoggingOptions optionsValue)
    {
        if (string.IsNullOrWhiteSpace(payload) || optionsValue == null)
            return payload;

        if (optionsValue.MaskSensitiveData == false || optionsValue.SensitiveFields == null || optionsValue.SensitiveFields.Length == 0)
            return payload;

        var trimmed = payload.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                var node = JsonNode.Parse(payload, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
                if (node != null)
                {
                    MaskJsonNode(node, optionsValue.SensitiveFields, optionsValue.MaskWith, optionsValue.SensitiveFieldMatchMode);
                    return node.ToJsonString(new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                }
            }
            catch { }
        }

        return MaskPlainText(payload, optionsValue.SensitiveFields, optionsValue.MaskWith);
    }

    private static void MaskJsonNode(JsonNode node, string[] sensitiveFields, string maskWith, SensitiveFieldMatchMode matchMode)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var key in jsonObject.Select(I => I.Key).ToList())
            {
                var val = jsonObject[key];

                if (MatchesSensitiveField(key, sensitiveFields, matchMode))
                {
                    jsonObject[key] = maskWith;
                    continue;
                }

                if (val is JsonObject || val is JsonArray)
                {
                    MaskJsonNode(val!, sensitiveFields, maskWith, matchMode);
                }
                else if (val is JsonValue)
                {
                    if (string.IsNullOrEmpty(val?.ToString()) == false && MatchesSensitiveField(key, sensitiveFields, matchMode))
                        jsonObject[key] = maskWith;
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is JsonObject || item is JsonArray)
                    MaskJsonNode(item!, sensitiveFields, maskWith, matchMode);
            }
        }
    }

    private static bool MatchesSensitiveField(string key, string[] sensitiveFields, SensitiveFieldMatchMode mode)
    {
        foreach (var sensitiveField in sensitiveFields)
        {
            if (string.IsNullOrWhiteSpace(sensitiveField)) continue;

            switch (mode)
            {
                case SensitiveFieldMatchMode.Equals:
                    if (string.Equals(key, sensitiveField, StringComparison.OrdinalIgnoreCase)) return true;
                    break;
                case SensitiveFieldMatchMode.StartsWith:
                    if (key.StartsWith(sensitiveField, StringComparison.OrdinalIgnoreCase)) return true;
                    break;
                default:
                    if (key.Contains(sensitiveField, StringComparison.OrdinalIgnoreCase)) return true;
                    break;
            }
        }

        return false;
    }

    private static string MaskPlainText(string text, string[] sensitiveFields, string maskWith)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        result = PlainJsonKeyValueRegex.Replace(result, match =>
        {
            var k = match.Groups["k"].Value;
            var v = match.Groups["v"].Value;

            if (sensitiveFields.Any(I => k.Contains(I, StringComparison.OrdinalIgnoreCase)))
                return match.Value.Replace($"\"{v}\"", $"\"{maskWith}\"");

            return match.Value;
        });

        result = KeyEqualsValueRegex.Replace(result, match =>
        {
            var k = match.Groups["k"].Value;

            if (sensitiveFields.Any(I => k.Contains(I, StringComparison.OrdinalIgnoreCase)))
                return $"{k}={maskWith}";

            return match.Value;
        });

        return result;
    }

    #endregion
}
