using System.Text.Json;
using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public sealed class PlayerProfile
{
    public int Version { get; set; } = 1;
    public string Language { get; set; } = "zh";
    public int BestKills { get; set; }
    public int BestGrazes { get; set; }
    public int CompletedRuns { get; set; }
    public int Victories { get; set; }
    public float FastestVictory { get; set; }
    public int LongRunVictories { get; set; }
    public float FastestLongVictory { get; set; }
    public int BestEndlessRounds { get; set; }
    public float BestEndlessTime { get; set; }
    public string LastStance { get; set; } = "";
    public bool MusicEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool ReducedMotion { get; set; }
    public float MasterVolume { get; set; } = 1;
    public float MusicVolume { get; set; } = 1;
    public float SoundVolume { get; set; } = 1;
    public VideoPreferences Video { get; set; } = new();
    public Dictionary<string, long[]> Bindings { get; set; } = GameControls.DefaultBindings();
    public int TouchMode { get; set; }
}

public sealed class ProfileStore
{
    public PlayerProfile Data { get; private set; } = new();
    public string Warning { get; private set; } = "";
    public string Notice => GameText.Get(Warning.Length > 0 ? Warning : GamePlatform.StorageNotice);
    private readonly string path;

    public ProfileStore(string? customPath = null)
    {
        path = customPath ?? ProjectSettings.GlobalizePath("user://rebirth/profile.json");
        try
        {
            if (!Godot.FileAccess.FileExists(path)) return;
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file == null) throw new IOException($"Profile open failed: {Godot.FileAccess.GetOpenError()}");
            var loaded = JsonSerializer.Deserialize(file.GetAsText(), ProfileJsonContext.Default.PlayerProfile);
            if (loaded == null || loaded.Version != 1 || loaded.CompletedRuns < 0 || loaded.Victories < 0 || loaded.BestKills < 0 || !float.IsFinite(loaded.FastestVictory))
                throw new InvalidDataException("Unsupported or invalid profile");
            loaded.MasterVolume = NormalizeVolume(loaded.MasterVolume);
            loaded.MusicVolume = NormalizeVolume(loaded.MusicVolume);
            loaded.SoundVolume = NormalizeVolume(loaded.SoundVolume);
            loaded.Video ??= new();
            loaded.Video.Normalize();
            loaded.Bindings = GameControls.NormalizeBindings(loaded.Bindings);
            loaded.TouchMode = Math.Clamp(loaded.TouchMode, 0, 2);
            loaded.Language = GameText.NormalizeLanguage(loaded.Language);
            loaded.LongRunVictories = Math.Max(0, loaded.LongRunVictories);
            loaded.BestEndlessRounds = Math.Max(0, loaded.BestEndlessRounds);
            loaded.FastestLongVictory = float.IsFinite(loaded.FastestLongVictory) ? Math.Max(0, loaded.FastestLongVictory) : 0;
            loaded.BestEndlessTime = float.IsFinite(loaded.BestEndlessTime) ? Math.Max(0, loaded.BestEndlessTime) : 0;
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
        if (run.HasStandardVictory || run.Phase == RunPhase.Won)
        {
            Data.Victories++;
            var time = run.HasStandardVictory ? run.StandardVictoryTime : run.Time;
            if (Data.FastestVictory <= 0 || time < Data.FastestVictory) Data.FastestVictory = time;
            if (run.HasStandardVictory)
            {
                Data.LongRunVictories++;
                if (Data.FastestLongVictory <= 0 || time < Data.FastestLongVictory) Data.FastestLongVictory = time;
            }
        }
        Data.LastStance = run.Build.Stance.ToString();
        Save();
    }

    public void RecordContinuation(RunState run)
    {
        if (!run.HasStandardVictory || !run.IsEndless) return;
        Data.BestEndlessRounds = Math.Max(Data.BestEndlessRounds, run.EndlessRounds);
        Data.BestEndlessTime = Math.Max(Data.BestEndlessTime, run.EndlessTime);
        Data.BestKills = Math.Max(Data.BestKills, run.Kills);
        Data.BestGrazes = Math.Max(Data.BestGrazes, run.Grazes);
        Data.LastStance = run.Build.Stance.ToString();
        Save();
    }

    public void Save()
    {
        if (Warning.Length > 0) return;
        try
        {
            var separator = path.LastIndexOfAny(['/', '\\']);
            var directory = separator < 0 ? "." : path[..separator];
            var created = DirAccess.MakeDirRecursiveAbsolute(directory);
            if (created != Error.Ok) throw new IOException($"Profile directory failed: {created}");
            var temporary = path + ".tmp";
            using (var file = Godot.FileAccess.Open(temporary, Godot.FileAccess.ModeFlags.Write))
            {
                if (file == null) throw new IOException($"Profile write failed: {Godot.FileAccess.GetOpenError()}");
                file.StoreString(JsonSerializer.Serialize(Data, ProfileJsonContext.Default.PlayerProfile));
                file.Flush();
                if (file.GetError() != Error.Ok) throw new IOException($"Profile flush failed: {file.GetError()}");
            }
            var renamed = DirAccess.RenameAbsolute(temporary, path);
            if (renamed != Error.Ok) throw new IOException($"Profile replace failed: {renamed}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Warning = "记录保存失败，本局仍可继续。";
            GD.PushWarning($"{Warning} {error.Message}");
        }
    }
}
