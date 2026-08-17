using System.Security.Cryptography;
using System.Text;

namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 为清单和本局注册表生成稳定的小写 SHA-256 指纹，供诊断与复现使用。
/// </summary>
public static class ContentFingerprint
{
    /// <summary>按 UTF-8 原文计算单个清单指纹，格式变化也会被视作不同构建输入。</summary>
    public static string HashText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>用长度前缀组合有序字段后计算指纹，避免不同字段连接成相同文本。</summary>
    public static string HashParts(IEnumerable<string> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var builder = new StringBuilder();
        foreach (string part in parts)
        {
            string safe = part ?? throw new InvalidOperationException(
                "Content fingerprint input cannot contain null values.");
            builder.Append(safe.Length).Append(':').Append(safe).Append('\n');
        }

        return HashText(builder.ToString());
    }
}
