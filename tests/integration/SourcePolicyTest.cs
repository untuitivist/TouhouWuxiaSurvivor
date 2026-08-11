using System.Text;
using Godot;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 审计项目文本编码与 C# 有效行数，把 UTF-8 无 BOM 和单文件 250 行上限固化为持续契约。
/// </summary>
public partial class SourcePolicyTest : Node
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".json", ".md", ".tscn", ".tres", ".cfg",
        ".godot", ".import", ".sha256",
    };

    /// <summary>
    /// 枚举受管项目文件并依次执行编码与有效行检查，失败时以非零状态返回具体文件。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            string projectRoot = ProjectSettings.GlobalizePath("res://");
            string[] textFiles = EnumerateTextFiles(projectRoot).ToArray();
            VerifyEncoding(textFiles);
            string[] codeFiles = EnumerateCodeFiles(projectRoot).ToArray();
            VerifyLineLimits(codeFiles);
            GD.Print($"Source policy test passed: {textFiles.Length} text files, " +
                $"{codeFiles.Length} C# files.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 递归枚举项目文本，同时排除 Git、Godot 缓存、构建输出和发行物等非源码目录。
    /// </summary>
    private static IEnumerable<string> EnumerateTextFiles(string projectRoot) =>
        Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .Where(path => TextExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !IsExcluded(projectRoot, path))
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// 仅枚举源码、测试和工具三类 C# 文件，避免把 Godot 或 MSBuild 生成代码纳入架构上限。
    /// </summary>
    private static IEnumerable<string> EnumerateCodeFiles(string projectRoot)
    {
        foreach (string directory in new[] { "src", "tests", "tools" })
        {
            string root = Path.Combine(projectRoot, directory);
            foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                yield return path;
            }
        }
    }

    /// <summary>
    /// 判断路径是否属于缓存、版本库、编译输出或发布目录，统一使用正斜杠避免平台差异。
    /// </summary>
    private static bool IsExcluded(string projectRoot, string path)
    {
        string relative = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
        return relative.StartsWith(".git/", StringComparison.Ordinal) ||
            relative.StartsWith(".godot/", StringComparison.Ordinal) ||
            relative.StartsWith("release/", StringComparison.Ordinal) ||
            relative.Contains("/bin/", StringComparison.Ordinal) ||
            relative.Contains("/obj/", StringComparison.Ordinal);
    }

    /// <summary>
    /// 拒绝 UTF-8 BOM、损坏字节序列和依赖系统代码页的文本，确保 Windows 与 Godot 读取一致。
    /// </summary>
    private static void VerifyEncoding(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF &&
                bytes[1] == 0xBB && bytes[2] == 0xBF;
            Require(!hasBom, $"UTF-8 BOM is forbidden: {path}.");
            _ = StrictUtf8.GetString(bytes);
        }
    }

    /// <summary>
    /// 排除空行、普通行注释与 XML 文档注释后统计有效代码，并报告全部超限文件及实际行数。
    /// </summary>
    private static void VerifyLineLimits(IEnumerable<string> paths)
    {
        string[] violations = paths
            .Select(path => (path, count: CountEffectiveLines(path)))
            .Where(item => item.count > 250)
            .Select(item => $"{item.path}: {item.count}")
            .ToArray();
        Require(violations.Length == 0,
            "C# effective line limit exceeded:\n" + string.Join('\n', violations));
    }

    /// <summary>
    /// 统计含实际语句或声明的行；当前项目不使用块注释，因此以完整的双斜杠注释行为排除边界。
    /// </summary>
    private static int CountEffectiveLines(string path) => File.ReadLines(path)
        .Select(line => line.TrimStart())
        .Count(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal));

    /// <summary>
    /// 将任一源码策略违约转换为包含全部定位信息的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
