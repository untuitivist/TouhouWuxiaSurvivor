using System.Globalization;

namespace Rebirth.Core;

public static partial class GameText
{
    public static string Language { get; private set; } = "zh";
    public static bool IsEnglish => Language == "en";
    private static readonly TextFormatter formatter = new();

    public static string NormalizeLanguage(string? language)
        => language?.Trim().Replace('_', '-').Split('-')[0].Equals("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "zh";

    public static void SetLanguage(string? language) => Language = NormalizeLanguage(language);

    public static string Get(string text)
    {
        if (IsEnglish) return English.TryGetValue(text, out var translated) ? translated : TranslateSource(text, 0);
        return Chinese.TryGetValue(text, out var revised) ? revised : TranslateSource(text, 0);
    }

    public static string Format(FormattableString text)
        => string.Format(formatter, Get(text.Format), text.GetArguments());

    private sealed class TextFormatter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? type) => type == typeof(ICustomFormatter) ? this : CultureInfo.InvariantCulture.GetFormat(type!);
        public string Format(string? format, object? argument, IFormatProvider? provider)
            => argument is string text ? Get(text) : argument is IFormattable value
                ? value.ToString(format, CultureInfo.InvariantCulture) ?? "" : argument?.ToString() ?? "";
    }
}
