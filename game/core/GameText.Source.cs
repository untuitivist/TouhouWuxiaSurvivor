using System.Text.RegularExpressions;

namespace Rebirth.Core;

public static partial class GameText
{
    private static readonly Dictionary<string, string> sourceCache = new(StringComparer.Ordinal);
    private static readonly Lazy<SourceTemplate[]> sourceTemplates = new(() => English
        .Where(entry => Placeholder!.IsMatch(entry.Key))
        .Select(entry => new SourceTemplate(entry.Key))
        .OrderByDescending(template => template.LiteralLength).ToArray());
    private static readonly Regex Placeholder = new(@"\{(?<index>\d+)(?:,[^}:]+)?(?::[^}]+)?\}", RegexOptions.CultureInvariant);

    private static string TranslateSource(string text, int depth)
    {
        if (depth > 8 || !text.Any(character => character is >= '\u3400' and <= '\u9fff')) return text;
        var cacheKey = Language + "|" + text;
        if (sourceCache.TryGetValue(cacheKey, out var cached)) return cached;
        var translations = IsEnglish ? English : Chinese;
        foreach (var template in sourceTemplates.Value)
        {
            if (!translations.TryGetValue(template.Source, out var output)) continue;
            var match = template.Pattern.Match(text);
            if (!match.Success) continue;
            var arguments = new object[template.ArgumentCount];
            for (var index = 0; index < arguments.Length; index++)
            {
                var value = match.Groups["arg" + index].Value;
                arguments[index] = translations.TryGetValue(value, out var translated) ? translated : TranslateSource(value, depth + 1);
            }
            var result = string.Format(System.Globalization.CultureInfo.InvariantCulture, output, arguments);
            if (sourceCache.Count >= 1024) sourceCache.Clear();
            sourceCache[cacheKey] = result;
            return result;
        }
        return text;
    }

    private sealed class SourceTemplate
    {
        public string Source { get; }
        public Regex Pattern { get; }
        public int ArgumentCount { get; }
        public int LiteralLength { get; }

        public SourceTemplate(string source)
        {
            Source = source;
            var pattern = new System.Text.StringBuilder(@"\A");
            var offset = 0;
            foreach (Match placeholder in Placeholder.Matches(source))
            {
                var literal = source[offset..placeholder.Index];
                LiteralLength += literal.Length;
                pattern.Append(Regex.Escape(literal));
                var index = int.Parse(placeholder.Groups["index"].Value, System.Globalization.CultureInfo.InvariantCulture);
                ArgumentCount = Math.Max(ArgumentCount, index + 1);
                pattern.Append("(?<arg" + index + ">.+?)");
                offset = placeholder.Index + placeholder.Length;
            }
            LiteralLength += source.Length - offset;
            pattern.Append(Regex.Escape(source[offset..])).Append(@"\z");
            Pattern = new(pattern.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        }
    }
}
