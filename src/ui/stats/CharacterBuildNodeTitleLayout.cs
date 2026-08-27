using System.Globalization;
using Godot;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 依据真实字体宽度压缩构筑节点标题，统一处理中英文、组合字符和极端长名称。
/// </summary>
internal static class CharacterBuildNodeTitleLayout
{
    /// <summary>
    /// 优先降低字号保留完整标题；最小字号仍超宽时，在完整文本元素边界追加明确省略号。
    /// </summary>
    public static (string Text, int FontSize) Fit(
        Font font,
        string text,
        float availableWidth,
        int preferredFontSize,
        int minimumFontSize)
    {
        int preferred = Math.Max(1, preferredFontSize);
        int minimum = Math.Clamp(minimumFontSize, 1, preferred);
        for (int size = preferred; size >= minimum; size--)
        {
            if (Measure(font, text, size) <= availableWidth)
            {
                return (text, size);
            }
        }

        const string suffix = "...";
        int[] elements = StringInfo.ParseCombiningCharacters(text);
        for (int count = elements.Length; count > 0; count--)
        {
            int boundary = count == elements.Length ? text.Length : elements[count];
            string candidate = text[..boundary].TrimEnd() + suffix;
            if (Measure(font, candidate, minimum) <= availableWidth)
            {
                return (candidate, minimum);
            }
        }

        return (suffix, minimum);
    }

    /// <summary>使用 Godot 当前字体整形结果测量单行标题宽度。</summary>
    private static float Measure(Font font, string text, int fontSize) =>
        font.GetStringSize(text, HorizontalAlignment.Left, -1.0f, fontSize).X;
}
