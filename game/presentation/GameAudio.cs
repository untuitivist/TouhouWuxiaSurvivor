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
            [EffectKind.Burst] = "explosion.wav",
            [EffectKind.Dash] = "shot.wav",
            [EffectKind.Victory] = "pickup.wav"
        };
        foreach (var entry in bindings) clips[entry.Key] = GD.Load<AudioStream>($"res://assets/internal_original/base/audio/{entry.Value}");
        for (var index = 0; index < 8; index++) { var voice = new AudioStreamPlayer(); AddChild(voice); voices.Add(voice); }
    }

    public void Apply(PlayerProfile profile)
    {
        SoundEnabled = profile.SoundEnabled;
        if (profile.MusicEnabled && !music.Playing) music.Play();
        if (!profile.MusicEnabled) music.Stop();
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
            voice.VolumeDb = entry.Kind == EffectKind.Hit ? -26 : -16;
            voice.PitchScale = entry.Kind == EffectKind.Dash ? 1.4f : 1;
            voice.Play();
        }
    }
}
