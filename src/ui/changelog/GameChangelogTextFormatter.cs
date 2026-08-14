using System.Text;
using TouhouWuxiaSurvivor.Versioning.Changelog;

namespace TouhouWuxiaSurvivor.Ui.Changelog;

/// <summary>
/// 把结构化版本条目投影为 RichTextLabel 的武侠配色正文和便于测试的纯文本正文。
/// </summary>
public static class GameChangelogTextFormatter
{
    /// <summary>生成带分节层级、朱砂项目符号和段间留白的受控 BBCode。</summary>
    public static string ToBbCode(GameChangelogEntry entry)
    {
        var builder = new StringBuilder();
        foreach (GameChangelogSection section in entry.Sections)
        {
            builder.Append("[font_size=13][color=#d0b978][b]")
                .Append(EscapeBbCode(section.Heading))
                .AppendLine("[/b][/color][/font_size]");
            foreach (string item in section.Items)
            {
                builder.Append("[color=#b9463d]◆[/color]  ")
                    .Append(EscapeBbCode(item))
                    .AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>生成不含显示标记的正文，供无头测试和可访问性状态直接检查。</summary>
    public static string ToPlainText(GameChangelogEntry entry) => string.Join(
        "\n\n",
        entry.Sections.Select(section => section.Heading + "\n" +
            string.Join('\n', section.Items.Select(item => "◆ " + item))));

    /// <summary>逐字符转义方括号，防止日志中的代码或玩家输入意外成为 RichTextLabel 标签。</summary>
    private static string EscapeBbCode(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            builder.Append(character switch
            {
                '[' => "[lb]",
                ']' => "[rb]",
                _ => character.ToString(),
            });
        }

        return builder.ToString();
    }
}
