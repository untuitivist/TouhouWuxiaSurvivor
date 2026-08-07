using System.Text;
using System.Text.Json;
using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;

/// <summary>
/// 以 UTF-8 无 BOM JSON 加载档案，并通过同目录临时文件与替换实现原子保存。
/// </summary>
public sealed class JsonProgressionProfileStore : IProgressionProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    /// <summary>
    /// 接收已展开的绝对路径，便于默认 user 目录和测试目录使用同一实现。
    /// </summary>
    public JsonProgressionProfileStore(string absolutePath) => _path = absolutePath;

    /// <summary>
    /// 从磁盘反序列化并修复档案；文件缺失、空白或损坏时返回完整默认档案。
    /// </summary>
    public ProgressionProfileData Load()
    {
        try
        {
            ProgressionProfileData profile = File.Exists(_path)
                ? JsonSerializer.Deserialize<ProgressionProfileData>(
                    File.ReadAllText(_path, Encoding.UTF8), JsonOptions)
                    ?? ProgressionProfileData.CreateDefault()
                : ProgressionProfileData.CreateDefault();
            profile.Repair();
            return profile;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"读取神社整备档案失败，已使用默认值: {exception.Message}");
            return ProgressionProfileData.CreateDefault();
        }
    }

    /// <summary>
    /// 先完整写入临时文件再替换目标文件，任何异常都会保留旧档并返回失败。
    /// </summary>
    public bool TrySave(ProgressionProfileData profile)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = _path + ".tmp";
            string json = JsonSerializer.Serialize(profile, JsonOptions);
            File.WriteAllText(
                temporaryPath,
                json + System.Environment.NewLine,
                new UTF8Encoding(false));
            File.Move(temporaryPath, _path, true);
            return true;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"保存神社整备档案失败: {exception.Message}");
            return false;
        }
    }
}
