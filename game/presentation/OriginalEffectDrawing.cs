using Godot;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private readonly Dictionary<string, Texture2D> effectTextures = [];
    private static readonly Color[] SparkColors = [new("97dfff"), new("c7b4ff"), new("ffb3df"), new("ffe1a2"), new("b8f2d0")];

    private void LoadEffectArtwork()
    {
        foreach (var name in new[] { "master_spark", "marisa_cast", "reimu_seal_ink", "reimu_talisman", "reimu_aura", "ritual_array" })
            effectTextures[name] = GD.Load<Texture2D>($"{BaseArt}effects/{name}.png");
        effectTextures["star"] = GD.Load<Texture2D>($"{BaseArt}combat/star.png");
    }

    private void EffectSprite(string name, Vector2 position, Vector2 size, Color tint, float rotation = 0)
    {
        if (size.X < 0.5f || size.Y < 0.5f || tint.A <= 0) return;
        surface.DrawSetTransform(position, rotation);
        surface.DrawTextureRect(effectTextures[name], new(-size * 0.5f, size), false, tint);
        surface.DrawSetTransform(Vector2.Zero);
    }

    private Color SparkColor(float offset = 0)
    {
        var phase = ReducedMotion ? 0 : Clock * 1.5f + offset;
        var index = (int)phase % SparkColors.Length;
        return SparkColors[index].Lerp(SparkColors[(index + 1) % SparkColors.Length], phase - MathF.Floor(phase));
    }

    private void SpellBeam(Vector2 origin, Vector2 direction, float length, float halfWidth, Color tint)
    {
        var texture = effectTextures["master_spark"];
        var capLength = Math.Min(length * 0.2f, halfWidth * 2);
        surface.DrawSetTransform(origin, direction.Angle());
        surface.DrawTextureRectRegion(texture, new(0, -halfWidth, capLength, halfWidth * 2), new(0, 0, 128, 128), tint);
        surface.DrawTextureRectRegion(texture, new(capLength, -halfWidth, length - capLength, halfWidth * 2), new(128, 0, 128, 128), tint);
        surface.DrawSetTransform(Vector2.Zero);
    }
}
