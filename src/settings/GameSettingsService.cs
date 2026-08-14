using System.Text;
using System.Text.Json;
using Godot;

namespace TouhouWuxiaSurvivor.Settings;

/// <summary>
/// 负责加载、修复、应用和保存全局用户设置，是菜单与游戏运行时之间的唯一设置入口。
/// </summary>
public static class GameSettingsService
{
    private const string SettingsPath = "user://settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static bool _initialized;

    public static GameSettingsData Current { get; private set; } = GameSettingsData.CreateDefault();
    public static GameSettingsRepairReport LastVideoRepair { get; private set; } =
        GameSettingsRepairReport.UnchangedDefaults();

    /// <summary>
    /// 首次调用时读取持久化文件、补全缺失键位并应用全部设置；后续调用保持幂等。
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        Current = LoadOrDefault();
        RepairBindings();
        ApplyBindings();
        ApplyAudio();
        ApplyVideo();
        _initialized = true;
        if (LastVideoRepair.Changed)
        {
            Save();
        }
    }

    /// <summary>
    /// 将当前设置以 UTF-8 without BOM 写入 user://，写入失败时记录警告但不中断游戏。
    /// </summary>
    public static void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Current, JsonOptions);
            string path = ProjectSettings.GlobalizePath(SettingsPath);
            File.WriteAllText(path, json + System.Environment.NewLine, new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            GD.PushWarning($"保存设置失败: {exception.Message}");
        }
    }

    /// <summary>
    /// 更新指定操作的一个键位槽并立即重建 InputMap；无效操作、槽位或空键不会生效。
    /// </summary>
    public static bool SetBinding(string actionId, int slot, Key key)
    {
        if (slot is < 0 or > 1 || key == Key.None || !Current.Bindings.ContainsKey(actionId))
        {
            return false;
        }

        long keyValue = (long)key;
        long[] bindings = Current.Bindings[actionId];
        if (bindings[1 - slot] == keyValue)
        {
            return false;
        }

        bindings[slot] = keyValue;
        ApplyBindings();
        Save();
        return true;
    }

    /// <summary>
    /// 仅恢复全部输入动作的默认双槽绑定，立即应用并保存，不改变音频和视频设置。
    /// </summary>
    public static void ResetBindings()
    {
        GameSettingsData defaults = GameSettingsData.CreateDefault();
        Current.Bindings = defaults.Bindings;
        ApplyBindings();
        Save();
    }

    /// <summary>
    /// 将当前三个线性音量值应用到 Master、Music 和 SFX 音频总线。
    /// </summary>
    public static void ApplyAudio()
    {
        SetBusVolume("Master", Current.MasterVolume);
        SetBusVolume("Music", Current.MusicVolume);
        SetBusVolume("SFX", Current.SfxVolume);
    }

    /// <summary>
    /// 应用窗口模式、窗口分辨率、垂直同步和帧率上限；无头运行时跳过窗口操作。
    /// </summary>
    public static void ApplyVideo()
    {
        _ = VideoSettingsCatalog.Normalize(Current);
        Engine.MaxFps = Current.MaxFps;
        DisplayServer.WindowSetVsyncMode(Current.VsyncEnabled
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);

        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        ApplyWindowMode();
    }

    /// <summary>
    /// 用当前设置为每个游戏操作重建最多两个物理键事件，跳过未绑定槽位。
    /// </summary>
    public static void ApplyBindings()
    {
        foreach (InputActionDefinition definition in InputActionCatalog.All)
        {
            if (!InputMap.HasAction(definition.Id))
            {
                InputMap.AddAction(definition.Id, 0.2f);
            }

            InputMap.ActionEraseEvents(definition.Id);
            foreach (long keyValue in Current.Bindings[definition.Id])
            {
                if ((Key)keyValue == Key.None)
                {
                    continue;
                }

                InputMap.ActionAddEvent(definition.Id, new InputEventKey
                {
                    PhysicalKeycode = (Key)keyValue,
                });
            }
        }
    }

    /// <summary>
    /// 从磁盘反序列化设置；文件缺失、损坏或内容为空时返回完整默认值。
    /// </summary>
    private static GameSettingsData LoadOrDefault()
    {
        try
        {
            string path = ProjectSettings.GlobalizePath(SettingsPath);
            if (!File.Exists(path))
            {
                return GameSettingsData.CreateDefault();
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<GameSettingsData>(json, JsonOptions)
                ?? GameSettingsData.CreateDefault();
        }
        catch (Exception exception)
        {
            GD.PushWarning($"读取设置失败，已使用默认值: {exception.Message}");
            return GameSettingsData.CreateDefault();
        }
    }

    /// <summary>
    /// 补全旧设置缺失或长度错误的动作绑定，并钳制所有数值范围。
    /// </summary>
    private static void RepairBindings()
    {
        GameSettingsData defaults = GameSettingsData.CreateDefault();
        Current.Bindings ??= [];
        foreach (InputActionDefinition action in InputActionCatalog.All)
        {
            if (!Current.Bindings.TryGetValue(action.Id, out long[]? keys) || keys.Length != 2)
            {
                Current.Bindings[action.Id] = defaults.Bindings[action.Id];
            }
        }

        Current.MasterVolume = Math.Clamp(Current.MasterVolume, 0.0f, 1.0f);
        Current.MusicVolume = Math.Clamp(Current.MusicVolume, 0.0f, 1.0f);
        Current.SfxVolume = Math.Clamp(Current.SfxVolume, 0.0f, 1.0f);
        Current.WindowMode = Math.Clamp(Current.WindowMode, 0, 2);
        LastVideoRepair = VideoSettingsCatalog.Normalize(Current);
    }

    /// <summary>
    /// 创建缺失的音频总线，并把线性音量转换为分贝和静音状态。
    /// </summary>
    private static void SetBusVolume(string name, float linearVolume)
    {
        int busIndex = AudioServer.GetBusIndex(name);
        if (busIndex < 0)
        {
            AudioServer.AddBus();
            busIndex = AudioServer.BusCount - 1;
            AudioServer.SetBusName(busIndex, name);
        }

        float volume = Math.Clamp(linearVolume, 0.0f, 1.0f);
        AudioServer.SetBusMute(busIndex, volume <= 0.0001f);
        AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(Mathf.Max(volume, 0.0001f)));
    }

    /// <summary>
    /// 根据设置切换窗口化、无边框窗口或全屏，并在窗口化时居中到当前屏幕。
    /// </summary>
    private static void ApplyWindowMode()
    {
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
        if (Current.WindowMode == 2)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            return;
        }

        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        Vector2I size = Current.WindowMode == 1
            ? DisplayServer.ScreenGetSize()
            : new Vector2I(Current.ResolutionWidth, Current.ResolutionHeight);
        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetFlag(
            DisplayServer.WindowFlags.Borderless,
            Current.WindowMode == 1);
        DisplayServer.WindowSetPosition((DisplayServer.ScreenGetSize() - size) / 2);
    }
}
