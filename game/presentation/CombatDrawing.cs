using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private void DrawEnemies()
    {
        if (Run == null) return;
        foreach (var enemy in Run.Enemies)
        {
            var position = Palette.Vector(enemy.Position);
            if (position.DistanceSquaredTo(camera) > 850 * 850) continue;
            if (enemy.Character is { } character)
            {
                surface.DrawTextureRect(PixelSkin.Artwork("shadow"), new(position + new Vector2(-24, 0), new(48, 22)), false);
                Sprite(character == HeroKind.Reimu ? "players/reimu" : "players/marisa", position, 1.8f, 0,
                    enemy.Flash > 0 ? new Color(1.6f, 1.6f, 1.6f) : Colors.White);
            }
            if (enemy.Telegraph > 0 && enemy.Kind == EnemyKind.Charger)
            {
                var target = position + Palette.Vector(enemy.Aim) * 265;
                surface.DrawLine(position, target, Palette.Alpha(Palette.Red, 0.18f), 30);
                surface.DrawLine(position, target, Palette.Alpha(Palette.Red, 0.65f), 2);
            }



            if (enemy.Kind is EnemyKind.Elite or EnemyKind.Boss)
            {
                var color = enemy.Kind == EnemyKind.Boss ? Palette.Violet : Palette.Red;
                surface.DrawArc(position, enemy.Radius + 9, Clock, Clock + MathF.Tau, 36, Palette.Alpha(color, 0.6f), 2);
                if (enemy.Telegraph > 0) surface.DrawArc(position, 55 + enemy.Telegraph * 35, 0, MathF.Tau, 40, Palette.Alpha(color, 0.6f), 2);
                surface.DrawRect(new(position + new Vector2(-27, -65), new(54, 4)), Palette.Deep);
                surface.DrawRect(new(position + new Vector2(-27, -65), new(54 * Math.Max(0, enemy.Health / enemy.MaxHealth), 4)), color);
            }

        }
    }

    private void DrawPlayer()
    {
        if (Run == null) return;
        var player = Palette.Vector(Run.PlayerPosition);
        var orbitRank = Run.Ranks[(int)ArtKind.YinYang];
        if (Run.Hero == HeroKind.Reimu && orbitRank > 0)
        {
            surface.DrawArc(player, Run.OrbitRadius, 0, MathF.Tau, 64, Palette.Alpha(Palette.Jade, 0.10f), 1);
            for (var index = 0; index < ReimuAbilitySystem.OrbitCount(Run); index++)
            {
                var position = Palette.Vector(ReimuAbilitySystem.OrbitPosition(Run, index));
                if (Run.Reimu.Charging && index == ReimuAbilitySystem.OrbitCount(Run) - 1)
                    OriginalEffect("reimu_aura", position, Vector2.One * (38 + 22 * (1 - Run.Reimu.ChargeRemaining / ReimuTuning.OrbChargeDuration)), Palette.Alpha(Colors.White, 0.65f));
                OriginalEffect("reimu_aura", position, Vector2.One * 28, Palette.Alpha(Palette.Jade, 0.12f));
                Sprite("actors/yin_yang_orb", position + new Vector2(0, 7), 0.62f);
            }
        }
        surface.DrawTextureRect(PixelSkin.Artwork("shadow"), new(player + new Vector2(-19, 0), new(38, 19)), false);
        if (Run.DashDuration > 0)
            for (var index = 1; index <= 4; index++)
                Sprite(Run.Hero == HeroKind.Reimu ? "players/reimu" : "players/marisa", player - Palette.Vector(Run.Facing) * (index * 18), 1.45f, 0, Palette.Alpha(Palette.Jade, 0.35f - index * 0.06f));
        var playerTint = Run.Invulnerability > 0 && (int)(Clock * 20) % 2 == 0 ? new Color(1, 1, 1, 0.4f) : Colors.White;
        Sprite(Run.Hero == HeroKind.Reimu ? "players/reimu" : "players/marisa", player, 1.45f, 0, playerTint);

        if (Focused)
        {
            surface.DrawArc(player, 34, 0, MathF.Tau, 40, Palette.Alpha(Palette.Paper, 0.35f), 1);
            surface.DrawCircle(player, 6, Palette.Deep);
            surface.DrawCircle(player, 3.5f, Palette.Paper);
            surface.DrawArc(player, 8, 0, MathF.Tau, 24, Palette.Red, 1);
        }
        else surface.DrawCircle(player, 2, Palette.Alpha(Palette.Paper, 0.8f));
    }

    private void DrawEffects()
    {
        foreach (var effect in effects)
        {
            var position = Palette.Vector(effect.Entry.Position);
            var progress = effect.Age / effect.Duration;
            var color = Palette.Alpha(Palette.Paper, 1 - progress);
            switch (effect.Entry.Kind)
            {
                case EffectKind.Hit:
                    Text(((int)effect.Entry.Value).ToString(), position + new Vector2(-8, -30 - progress * 22), 13, Palette.Alpha(Palette.Gold, 1 - progress));
                    break;
                case EffectKind.Beam:
                    Star(position, 32 * (1 - progress), Palette.Alpha(Palette.Gold, 1 - progress), progress * 3);
                    break;
                case EffectKind.Explosion:
                    OriginalEffect("reimu_aura", position, Vector2.One * effect.Entry.Value * (0.3f + progress * 1.7f), Palette.Alpha(new Color("ffaec9"), 0.5f * (1 - progress)));
                    break;
                case EffectKind.Spell:
                    OriginalEffect(Run?.Hero == HeroKind.Reimu ? "reimu_aura" : "marisa_cast", position, Vector2.One * (48 + 172 * progress), Palette.Alpha(Colors.White, 0.45f * (1 - progress)));
                    break;
                case EffectKind.Seal:
                    OriginalEffect("ritual_array", position, Vector2.One * (150 + 170 * progress), Palette.Alpha(Colors.White, 0.35f * (1 - progress)));
                    break;
                case EffectKind.Graze:
                    Star(position, 8 + progress * 12, Palette.Alpha(Palette.Jade, 1 - progress), progress);
                    break;
                case EffectKind.Defeat:
                    for (var index = 0; index < 5; index++) Star(position + Vector2.FromAngle(index * MathF.Tau / 5) * progress * 25, 4 * (1 - progress), Palette.Alpha(Palette.Gold, 1 - progress), index);
                    break;
            }
        }
    }
}
