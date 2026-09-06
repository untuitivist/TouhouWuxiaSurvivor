using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameAudio : Node
{
    private readonly AudioStreamPlayer music = new();
    private readonly List<AudioStreamPlayer> voices = [];
    private readonly Dictionary<EffectKind, AudioStream> clips = [];
    private double lastHit;
    public bool SoundEnabled = true;
    private float soundVolume = 1;
    private bool activated = !GamePlatform.IsWeb;

    public void Activate(PlayerProfile profile, bool allowPlayback)
    {
        if (activated) return;
        activated = true;
        Apply(profile, allowPlayback);
    }

    public override void _Ready()
    {
        var song = GD.Load<AudioStreamOggVorbis>("res://assets/internal_original/base/audio/bgm_reimu.ogg");
        if (song != null)
        {
            song = (AudioStreamOggVorbis)song.Duplicate();
            song.Loop = true;
            music.Stream = song;
        }
        music.VolumeDb = -17;
        AddChild(music);
        var bindings = new Dictionary<EffectKind, string>
        {
            [EffectKind.Hit] = "enemy_hit.wav",
            [EffectKind.Hurt] = "player_hurt.wav",
            [EffectKind.Level] = "pickup.wav",
            [EffectKind.Seal] = "pickup.wav",
            [EffectKind.Spell] = "explosion.wav",
            [EffectKind.Beam] = "shot.wav",
            [EffectKind.Dash] = "shot.wav",
            [EffectKind.Victory] = "pickup.wav"
        };
        foreach (var entry in bindings) clips[entry.Key] = GD.Load<AudioStream>($"res://assets/internal_original/base/audio/{entry.Value}");
        for (var index = 0; index < 8; index++) { var voice = new AudioStreamPlayer(); AddChild(voice); voices.Add(voice); }
    }

    public void Apply(PlayerProfile profile, bool allowPlayback = true)
    {
        allowPlayback &= activated;
        SoundEnabled = profile.SoundEnabled;
        soundVolume = profile.SoundVolume;
        AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Math.Max(profile.MasterVolume, 0.0001f)));
        AudioServer.SetBusMute(0, profile.MasterVolume <= 0);
        music.VolumeLinear = Mathf.DbToLinear(-17) * profile.MusicVolume;
        foreach (var voice in voices)
        {
            if (!SoundEnabled) voice.Stop();
            voice.VolumeLinear = Mathf.DbToLinear(voice.GetMeta("base_volume", -16).AsSingle()) * soundVolume;
        }
        if (profile.MusicEnabled && allowPlayback && !music.Playing) music.Play();
        if (!profile.MusicEnabled || !allowPlayback) music.Stop();
    }

    public override void _ExitTree()
    {
        music.Stop();
        music.Stream = null;
        foreach (var voice in voices) { voice.Stop(); voice.Stream = null; }
    }

    public void PlayEvents(IReadOnlyList<CombatEvent> events)
    {
        if (!SoundEnabled) return;
        foreach (var entry in events)
        {
            if (!clips.TryGetValue(entry.Kind, out var clip)) continue;
            var now = Time.GetTicksMsec() / 1000.0;
            if (entry.Kind == EffectKind.Hit && now - lastHit < 0.11) continue;
            if (entry.Kind == EffectKind.Hit) lastHit = now;
            var voice = voices.Find(candidate => !candidate.Playing);
            if (voice == null) continue;
            voice.Stream = clip;
            var baseVolume = entry.Kind == EffectKind.Hit ? -26 : -16;
            voice.SetMeta("base_volume", baseVolume);
            voice.VolumeLinear = Mathf.DbToLinear(baseVolume) * soundVolume;
            voice.PitchScale = entry.Kind == EffectKind.Dash ? 1.4f : 1;
            voice.Play();
        }
    }
}
