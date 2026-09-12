using Godot;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private readonly Dictionary<string, Texture2D> effectTextures = [];
    internal const int StarColorFrames = 16;
    internal const int StarColorVariants = 14;
    private ShaderMaterial sparkMaterial = null!;

    internal static ShaderMaterial CreateSparkMaterial()
        => new() { Shader = GD.Load<Shader>("res://game/presentation/marisa_beam.gdshader") };

    internal static void ConfigureSparkMaterial(ShaderMaterial material, float clock, float length, float capLength, bool reducedMotion)
    {
        material.SetShaderParameter("flow_time", reducedMotion ? 0 : clock);
        material.SetShaderParameter("beam_length", length);
        material.SetShaderParameter("cap_length", capLength);
    }

    private void LoadOriginalEffects()
    {
        foreach (var name in new[] { "master_spark", "marisa_cast", "reimu_seal_ink", "reimu_talisman", "reimu_aura", "ritual_array" })
            effectTextures[name] = GD.Load<Texture2D>($"{BaseArt}effects/{name}.png");
        effectTextures["star"] = GD.Load<Texture2D>($"{BaseArt}combat/star.png");
        effectTextures["marisa_mushroom"] = GD.Load<Texture2D>($"{BaseArt}combat/marisa_mushroom.png");
    }

    private void OriginalEffect(string name, Vector2 position, Vector2 size, Color tint, float rotation = 0)
    {
        if (size.X < 0.5f || size.Y < 0.5f || tint.A <= 0) return;
        surface.DrawSetTransform(position, rotation);
        surface.DrawTextureRect(effectTextures[name], new(-size * 0.5f, size), false, tint);
        surface.DrawSetTransform(Vector2.Zero);
    }

    private void OriginalBeam(Vector2 origin, Vector2 direction, float length, float halfWidth, float capLength, Color tint)
    {
        var texture = effectTextures["master_spark"];
        surface.DrawSetTransform(origin, direction.Angle());
        surface.DrawTextureRectRegion(texture, new(0, -halfWidth, capLength, halfWidth * 2), new(0, 0, 128, 128), tint);
        surface.DrawTextureRectRegion(texture, new(capLength, -halfWidth, length - capLength, halfWidth * 2), new(128, 0, 128, 128), tint);
        surface.DrawSetTransform(Vector2.Zero);
    }
}
