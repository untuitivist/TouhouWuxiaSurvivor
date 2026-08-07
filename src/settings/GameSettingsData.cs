namespace TouhouWuxiaSurvivor.Settings;

/// <summary>
/// 表示可序列化的用户设置快照，只包含跨启动需要保留的稳定基础类型。
/// </summary>
public sealed class GameSettingsData
{
    public float MasterVolume { get; set; } = 0.8f;

    public float MusicVolume { get; set; } = 0.7f;

    public float SfxVolume { get; set; } = 0.8f;

    public int WindowMode { get; set; }

    public int ResolutionWidth { get; set; } = 1280;

    public int ResolutionHeight { get; set; } = 720;

    public bool VsyncEnabled { get; set; } = true;

    public int MaxFps { get; set; } = 60;

    public Dictionary<string, long[]> Bindings { get; set; } = [];

    /// <summary>
    /// 创建带有完整双槽默认绑定的新设置对象，避免调用方共享可变数组。
    /// </summary>
    public static GameSettingsData CreateDefault()
    {
        var settings = new GameSettingsData();
        foreach (InputActionDefinition action in InputActionCatalog.All)
        {
            settings.Bindings[action.Id] =
                [(long)action.PrimaryKey, (long)action.SecondaryKey];
        }

        return settings;
    }
}
