using System.Text;
using System.Text.Json;
using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public sealed class PlayerProfile
{
    public int Version { get; set; } = 1;
    public int BestKills { get; set; }
    public int BestGrazes { get; set; }
    public int CompletedRuns { get; set; }
    public int Victories { get; set; }
    public float FastestVictory { get; set; }
    public bool MusicEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool ReducedMotion { get; set; }
    public float MasterVolume { get; set; } = 1;
    public float MusicVolume { get; set; } = 1;
    public float SoundVolume { get; set; } = 1;
    public VideoPreferences Video { get; set; } = new();
    public Dictionary<string, long[]> Bindings { get; set; } = GameControls.DefaultBindings();
}

public sealed class ProfileStore
{
    public PlayerProfile Data { get; private set; } = new();
    public string Warning { get; private set; } = "";
    private readonly string path;

    public ProfileStore(string? customPath = null)
    {
        path = customPath ?? ProjectSettings.GlobalizePath("user://rebirth/profile.json");
        try
        {
            if (!System.IO.File.Exists(path)) return;
            var loaded = JsonSerializer.Deserialize<PlayerProfile>(System.IO.File.ReadAllText(path, Encoding.UTF8));
            if (loaded == null || loaded.Version != 1 || loaded.CompletedRuns < 0 || loaded.Victories < 0 || loaded.BestKills < 0 || !float.IsFinite(loaded.FastestVictory))
                throw new InvalidDataException("Unsupported or invalid profile");
            loaded.MasterVolume = NormalizeVolume(loaded.MasterVolume);
            loaded.MusicVolume = NormalizeVolume(loaded.MusicVolume);
            loaded.SoundVolume = NormalizeVolume(loaded.SoundVolume);
            loaded.Video ??= new();
            loaded.Video.Normalize();
            loaded.Bindings = GameControls.NormalizeBindings(loaded.Bindings);
            Data = loaded;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            Warning = "记录读取失败，本次使用默认设置；原文件保留。";
            GD.PushWarning($"{Warning} {error.Message}");
        }
    }

    private static float NormalizeVolume(float value) => float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 1;

    public void Record(RunState run)
    {
        if (run.Phase is not (RunPhase.Won or RunPhase.Lost)) return;
        Data.CompletedRuns++;
        Data.BestKills = Math.Max(Data.BestKills, run.Kills);
        Data.BestGrazes = Math.Max(Data.BestGrazes, run.Grazes);
        if (run.Phase == RunPhase.Won)
        {
            Data.Victories++;
            if (Data.FastestVictory <= 0 || run.Time < Data.FastestVictory) Data.FastestVictory = run.Time;
        }
        Save();
    }

    public void Save()
    {
        if (Warning.Length > 0) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            System.IO.File.WriteAllText(temporary, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            System.IO.File.Move(temporary, path, true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Warning = "记录保存失败，本局仍可继续。";
            GD.PushWarning($"{Warning} {error.Message}");
        }
    }
}
