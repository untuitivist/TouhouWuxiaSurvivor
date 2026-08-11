using System.Text.Json;
using Godot;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 作为 Godot 内部素材工具的场景入口，负责读取命令行、遍历主清单与作品清单，并统一报告构建成败。
/// </summary>
public partial class InternalPreviewAssetBuilder : Node
{
    private const string ManifestPath = "res://tools/internal_assets/build_manifest.json";
    private const string PackManifestRoot = "res://tools/internal_assets/packs";
    private const string OutputRoot = "res://assets/internal_original/";

    /// <summary>
    /// 建立一次构建会话，依序处理所有清单并写来源哈希；任何异常都会使工具场景返回非零退出码。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            WriteProgress("started", true);
            GD.Print("Internal asset builder: started.");
            string sourceRoot = ReadSourceRoot();
            var usedSources = new HashSet<string>(StringComparer.Ordinal);
            var context = new InternalAssetBuildContext(
                sourceRoot, OutputRoot, usedSources);
            var compiler = new InternalAssetManifestCompiler(context);

            using JsonDocument document = ReadManifest(ManifestPath);
            WriteProgress("manifest loaded");
            GD.Print("Internal asset builder: manifest loaded.");
            compiler.Build(document.RootElement);
            foreach (string path in EnumeratePackManifests())
            {
                using JsonDocument pack = ReadManifest(path);
                compiler.Build(pack.RootElement);
                GD.Print($"Internal asset builder: {Path.GetFileName(path)} complete.");
            }

            InternalSourceHashWriter.Write(sourceRoot, usedSources,
                ProjectSettings.GlobalizePath(OutputRoot + "source_files.sha256"));
            GD.Print($"Internal preview assets built from {usedSources.Count} source files.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 从 Godot 资源路径或磁盘路径读取一份 JSON 清单，并返回由调用者限定生命周期的已解析文档。
    /// </summary>
    private static JsonDocument ReadManifest(string path)
    {
        string json = path.StartsWith("res://", StringComparison.Ordinal)
            ? Godot.FileAccess.GetFileAsString(path)
            : File.ReadAllText(path);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// 按稳定文件名顺序枚举每作独立清单；目录不存在时只构建主清单以保持工具向后兼容。
    /// </summary>
    private static IEnumerable<string> EnumeratePackManifests()
    {
        string directory = ProjectSettings.GlobalizePath(PackManifestRoot);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal)
            : [];
    }

    /// <summary>
    /// 将少量阶段标记写入 Godot 用户目录，绕过控制台缓冲以便定位内部工具的阻塞位置。
    /// </summary>
    private static void WriteProgress(string message, bool reset = false)
    {
        string path = ProjectSettings.GlobalizePath(
            "user://internal-asset-builder-progress.txt");
        string line = $"{DateTime.Now:O} {message}{System.Environment.NewLine}";
        if (reset)
        {
            File.WriteAllText(path, line, new System.Text.UTF8Encoding(false));
            return;
        }

        File.AppendAllText(path, line, new System.Text.UTF8Encoding(false));
    }

    /// <summary>
    /// 从 Godot 用户参数读取外部素材根目录，并拒绝缺失路径，避免把空输出误判为构建成功。
    /// </summary>
    private static string ReadSourceRoot()
    {
        string[] arguments = OS.GetCmdlineUserArgs();
        for (int index = 0; index + 1 < arguments.Length; index++)
        {
            if (arguments[index] == "--source-root" &&
                Directory.Exists(arguments[index + 1]))
            {
                return Path.GetFullPath(arguments[index + 1]);
            }
        }

        throw new InvalidOperationException(
            "Missing valid --source-root for internal assets.");
    }
}
