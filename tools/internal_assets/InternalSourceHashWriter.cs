using System.Security.Cryptography;
using System.Text;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 为实际读取的外部原作文件写稳定 SHA-256 清单，使后续替换和源目录只读检查可机械复核。
/// </summary>
public static class InternalSourceHashWriter
{
    /// <summary>
    /// 按相对路径序排序并写入 UTF-8 无 BOM 文本，每行包含哈希和原始相对路径。
    /// </summary>
    public static void Write(
        string sourceRoot,
        IEnumerable<string> relativePaths,
        string destination)
    {
        var lines = new List<string>();
        foreach (string relative in relativePaths.OrderBy(value => value, StringComparer.Ordinal))
        {
            string source = Path.Combine(
                sourceRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            string digest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)))
                .ToLowerInvariant();
            lines.Add($"{digest}  {relative}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, string.Join('\n', lines) + "\n", new UTF8Encoding(false));
    }
}
