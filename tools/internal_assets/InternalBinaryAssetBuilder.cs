using Godot;
using System.Diagnostics;
using System.Text.Json;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 按内部素材清单复制无需图像变换的二进制资源，并把来源加入统一哈希审计集合。
/// </summary>
internal static class InternalBinaryAssetBuilder
{
    /// <summary>
    /// 验证每个输入文件后复制到项目内部目录；任何缺失或空文件都会终止构建，防止静默打包无效音频。
    /// </summary>
    internal static void Build(
        string sourceRoot,
        string outputRoot,
        JsonElement definitions,
        ISet<string> usedSources)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            string relativeSource = RequiredString(definition, "source");
            string relativeOutput = RequiredString(definition, "output");
            string source = Path.Combine(sourceRoot,
                relativeSource.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source) || new FileInfo(source).Length == 0)
            {
                throw new FileNotFoundException(
                    $"Internal binary source is missing or empty: {source}.", source);
            }

            string output = ProjectSettings.GlobalizePath(outputRoot + relativeOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            if (definition.TryGetProperty("stripMetadata", out JsonElement stripElement) &&
                stripElement.GetBoolean())
            {
                CopyWithoutMetadata(source, output);
            }
            else
            {
                File.Copy(source, output, true);
            }
            usedSources.Add(relativeSource);
        }
    }

    /// <summary>
    /// 通过 FFmpeg 流复制移除非标准旧编码注释，不重新编码音频包；工具缺失或退出失败时阻止产生带警告的资源。
    /// </summary>
    private static void CopyWithoutMetadata(string source, string output)
    {
        var start = new ProcessStartInfo("ffmpeg")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string argument in new[]
        {
            "-hide_banner", "-loglevel", "error", "-y", "-i", source,
            "-map_metadata", "-1", "-c:a", "copy", output,
        })
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start) ??
            throw new InvalidOperationException("Could not start FFmpeg for metadata cleanup.");
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length == 0)
        {
            throw new InvalidOperationException(
                $"FFmpeg metadata cleanup failed with exit {process.ExitCode}: {error}");
        }
    }

    /// <summary>读取清单必需字符串并拒绝空白路径，使错误停留在构建阶段而不是运行时。</summary>
    private static string RequiredString(JsonElement definition, string property)
    {
        string? value = definition.GetProperty(property).GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException($"Binary asset property '{property}' is empty.");
    }
}
