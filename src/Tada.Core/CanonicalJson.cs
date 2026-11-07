using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tada;

/// <summary>RFC 8785 JSON encoding. Input files themselves are never normalised.</summary>
public static class CanonicalJson
{
    public static byte[] Encode<T>(T value) => Encode(JsonSerializer.SerializeToElement(value, Data.Json));
    public static byte[] Encode(JsonElement value)
    {
        var builder = new StringBuilder();
        Write(value, builder);
        return new UTF8Encoding(false, true).GetBytes(builder.ToString());
    }
    public static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    public static string FileDigest(string path)
    {
        using var stream = File.OpenRead(path);
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream));
    }
    public static string Identity<T>(T value) => Digest(Encode(value));

    private static void Write(JsonElement element, StringBuilder text)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                text.Append('{');
                var properties = element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
                if (properties.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                    throw new TadaException("Duplicate JSON property.");
                for (int i = 0; i < properties.Length; i++)
                {
                    if (i > 0) text.Append(',');
                    String(properties[i].Name, text);
                    text.Append(':');
                    Write(properties[i].Value, text);
                }
                text.Append('}');
                break;
            case JsonValueKind.Array:
                text.Append('[');
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (index++ > 0) text.Append(',');
                    Write(item, text);
                }
                text.Append(']');
                break;
            case JsonValueKind.String: String(element.GetString()!, text); break;
            case JsonValueKind.Number: text.Append(Number(element.GetDouble())); break;
            case JsonValueKind.True: text.Append("true"); break;
            case JsonValueKind.False: text.Append("false"); break;
            case JsonValueKind.Null: text.Append("null"); break;
            default: throw new TadaException("Unsupported JSON value.");
        }
    }

    private static void String(string value, StringBuilder text)
    {
        text.Append('"');
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    throw new TadaException("Invalid Unicode surrogate.");
                text.Append(c).Append(value[++i]);
                continue;
            }
            if (char.IsLowSurrogate(c)) throw new TadaException("Invalid Unicode surrogate.");
            text.Append(c switch
            {
                '"' => "\\\"", '\\' => "\\\\", '\b' => "\\b", '\t' => "\\t", '\n' => "\\n",
                '\f' => "\\f", '\r' => "\\r", < ' ' => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture),
                _ => c.ToString()
            });
        }
        text.Append('"');
    }

    private static string Number(double value)
    {
        if (!double.IsFinite(value)) throw new TadaException("Non-finite JSON number.");
        if (value == 0) return "0";
        string sign = value < 0 ? "-" : "";
        var parts = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture).Split('E', 'e');
        int exponent = parts.Length == 2 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
        int point = parts[0].IndexOf('.');
        if (point < 0) point = parts[0].Length;
        string digits = parts[0].Replace(".", "");
        int decimalPosition = point + exponent;
        while (digits.Length > 1 && digits[0] == '0') { digits = digits[1..]; decimalPosition--; }
        digits = digits.TrimEnd('0');
        if (digits.Length == 0) return "0";
        int scientificExponent = decimalPosition - 1;
        if (scientificExponent >= -6 && scientificExponent < 21)
        {
            if (decimalPosition <= 0) return sign + "0." + new string('0', -decimalPosition) + digits;
            if (decimalPosition >= digits.Length) return sign + digits + new string('0', decimalPosition - digits.Length);
            return sign + digits[..decimalPosition] + "." + digits[decimalPosition..];
        }
        return sign + digits[0] + (digits.Length > 1 ? "." + digits[1..] : "") + "e" +
            (scientificExponent >= 0 ? "+" : "") + scientificExponent.ToString(CultureInfo.InvariantCulture);
    }
}
