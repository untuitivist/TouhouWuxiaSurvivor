using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas : Node2D
{
    public RunState? Run;
    public bool Focused;
    public bool ReducedMotion;
    public Font BodyFont = null!;
    public Font TitleFont = null!;
    public float Clock;
    private Vector2 camera;
    private float shake;
    private readonly Dictionary<string, Texture2D> textures = [];
    private readonly Dictionary<string, (int Size, int Frames)> spriteFrames = [];
    private Texture2D titleLandscape = null!;
    private Texture2D toriiTexture = null!;
    private Texture2D[] treeCanopies = [];
    private readonly List<VisualEvent> effects = [];
    private const string BaseArt = "res://assets/internal_original/base/";

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        titleLandscape = GD.Load<Texture2D>("res://assets/ui/title/moonlit_shrine.png");
        toriiTexture = PixelLandscape.Load("torii");
        treeCanopies = [PixelLandscape.Load("tree_canopy_a"), PixelLandscape.Load("tree_canopy_b")];
        foreach (var name in new[] { "players/reimu", "players/marisa", "actors/kedama", "actors/wild_fairy", "actors/mountain_spirit", "actors/great_youkai", "actors/yin_yang_orb" })
            textures[name] = GD.Load<Texture2D>($"{BaseArt}{name}.png");
        textures["path"] = PixelSkin.Artwork("stone");
        foreach (var entry in textures) spriteFrames[entry.Key] = (entry.Value.GetHeight(), Math.Max(1, entry.Value.GetWidth() / entry.Value.GetHeight()));
        LoadOriginalEffects();
        InitializeRenderLayers();
    }

    public void ResetView()
    {
        camera = CameraTarget();
        effects.Clear();
        shake = 0;
        renderDirty = true;
    }

    public void ReceiveEvents()
    {
        if (Run == null) return;
        foreach (var entry in Run.Events)
        {
            if (effects.Count >= 180) break;
            if (entry.Kind == EffectKind.Hit && effects.Count > 75) continue;
            var duration = entry.Kind switch { EffectKind.Hit => 0.48f, EffectKind.Beam => 0.2f, EffectKind.Spell => 0.65f, EffectKind.Seal => 1.2f, _ => 0.4f };
            effects.Add(new(entry, duration));
            if (entry.Kind is EffectKind.Hurt or EffectKind.Spell) shake = entry.Kind == EffectKind.Hurt ? 6 : 9;
        }
    }

    public override void _Process(double delta)
    {
        var elapsed = (float)Math.Min(delta, 0.1);
        if (Run == null || Run.Phase == RunPhase.Playing) Clock += elapsed;
        if (Run != null)
        {
            camera = camera.Lerp(CameraTarget(), 1 - MathF.Exp(-elapsed * 12));
            if (Run.Phase == RunPhase.Playing)
            {
                foreach (var effect in effects) effect.Age += elapsed;
                effects.RemoveAll(effect => effect.Age >= effect.Duration);
            }
        }
        shake = Math.Max(0, shake - elapsed * 25);
        UpdateRenderLayers(elapsed);
    }

    public override void _Draw()
    {
        if (BodyFont == null) return;
        surface.DrawRect(new(0, 0, 1280, 720), Palette.Deep);
        if (Run == null) { DrawTitleLandscape(); return; }
    }

    private Vector2 CameraTarget()
    {
        if (Run == null) return Vector2.Zero;
        return Palette.Vector(Run.PlayerPosition).Clamp(new Vector2(-RunState.ArenaHalfWidth + 640, -RunState.ArenaHalfHeight + 282), new Vector2(RunState.ArenaHalfWidth - 640, RunState.ArenaHalfHeight - 282));
    }

    private void Text(string text, Vector2 position, int size, Color color, Font? font = null)
        => surface.DrawString(font ?? BodyFont, position, text, HorizontalAlignment.Left, -1, size, color);

    private void FittedText(string text, Vector2 position, float width, int size, Color color)
    {
        while (size > 10 && BodyFont.GetStringSize(text, fontSize: size).X > width) size--;
        surface.DrawString(BodyFont, position, text, HorizontalAlignment.Left, width, size, color);
    }

    private void CenterText(string text, Vector2 position, int size, Color color, Font? font = null)
    {
        var selected = font ?? BodyFont;
        position.X -= selected.GetStringSize(text, fontSize: size).X / 2;
        Text(text, position, size, color, selected);
    }

    private void Sprite(string name, Vector2 position, float scale, float phase = 0, Color? tint = null)
    {
        var texture = textures[name];
        var size = texture.GetHeight();
        var frameCount = Math.Max(1, texture.GetWidth() / size);
        var frame = (int)(Clock * 9 + phase) % frameCount;
        surface.DrawTextureRectRegion(texture, new(position - new Vector2(size * scale / 2, size * scale * 0.75f), new(size * scale, size * scale)), new(frame * size, 0, size, size), tint ?? Colors.White);
    }

    private void Diamond(Vector2 position, float size, Color color)
    {
        if (size < 0.25f || color.A <= 0) return;
        surface.DrawColoredPolygon([position + new Vector2(0, -size), position + new Vector2(size, 0), position + new Vector2(0, size), position + new Vector2(-size, 0)], color);
    }

    private sealed class VisualEvent(CombatEvent entry, float duration)
    {
        public readonly CombatEvent Entry = entry;
        public readonly float Duration = duration;
        public float Age;
    }
}
