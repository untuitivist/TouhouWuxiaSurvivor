using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private SpriteBatch actorBatch = null!;
    private readonly HeroAnimation heroAnimation = new();
    private readonly List<ActorInstance> actorInstances = new(1024);
    private (ActorArtwork Art, float Scale)[] enemyStyles = [];

    private void InitializeActorBatch()
    {
        actorBatch = new SpriteBatch(actorAtlas.Texture) { ZIndex = 6 };
        worldLayer.AddChild(actorBatch);
        enemyStyles = Enum.GetValues<EnemyKind>().Select(kind =>
        {
            var name = kind switch { EnemyKind.Kedama => "actors/kedama", EnemyKind.Fairy => "actors/wild_fairy", EnemyKind.Charger => "actors/mountain_spirit", _ => "actors/great_youkai" };
            var scale = kind switch { EnemyKind.Kedama => 0.95f, EnemyKind.Elite => 1.7f, EnemyKind.Boss => 2f, _ => 1.15f };
            return (actorAtlas[name], scale);
        }).ToArray();
    }

    private void QueueActor(ActorArtwork artwork, Vector2 foot, float scale, Color tint, int frame = 0, int row = 0, bool mirror = false)
    {
        var size = artwork.FrameSize * scale;
        if (!InView(foot, Math.Max(size.X, size.Y) + 30)) return;
        actorInstances.Add(new(foot, new(size.X, size.Y * WorldProjection.UprightScale), artwork.FootAnchor * scale * WorldProjection.UprightScale,
            artwork.Region(frame, row), tint, mirror, actorInstances.Count));
    }

    private void UpdateActorBatch()
    {
        if (Run == null) return;
        actorInstances.Clear();
        QueueSceneryActors();
        foreach (var enemy in Run.Enemies)
        {
            var style = enemyStyles[(int)enemy.Kind];
            QueueActor(style.Art, Palette.Vector(enemy.Position), style.Scale, enemy.Flash > 0 ? new Color(1.8f, 1.8f, 1.8f) : Colors.White,
                (int)(Clock * 9 + enemy.Id * 0.7f) % style.Art.Columns);
        }
        var player = Palette.Vector(Run.PlayerPosition);
        var hero = actorAtlas[Run.Hero == HeroKind.Reimu ? "players/reimu" : "players/marisa"];
        var moving = Run.PlayerVelocity.LengthSquared() > 1;
        var row = WorldProjection.FacingRow(Run.Beam?.Direction ?? Run.Facing);
        var frame = heroAnimation.Frame(Run.Time, Run.PrimaryCastCooldown, moving, Run.Beam != null, Run.Beam?.Warmup > 0);
        if (Run.DashDuration > 0)
            for (var index = 4; index >= 1; index--)
                QueueActor(hero, player - Palette.Vector(Run.Facing) * (index * 18), 1.45f, Palette.Alpha(Palette.Jade, 0.35f - index * 0.06f), frame, row);
        QueueActor(hero, player, 1.45f, Run.Invulnerability > 0 && (int)(Clock * 20) % 2 == 0 ? new Color(1, 1, 1, 0.4f) : Colors.White, frame, row);
        if (Run.Hero == HeroKind.Reimu && Run.Ranks[(int)ArtKind.YinYang] > 0)
            for (var index = 0; index < ReimuAbilitySystem.OrbitCount(Run); index++)
                QueueActor(actorAtlas["actors/yin_yang_orb"], Palette.Vector(ReimuAbilitySystem.OrbitPosition(Run, index)), 0.92f, Colors.White, (int)(Clock * 9));
        actorInstances.Sort();
        actorBatch.Begin();
        foreach (var actor in actorInstances)
            actorBatch.AddRegion(actor.Foot + new Vector2(0, actor.Size.Y * 0.5f - actor.Anchor), actor.Size, actor.Tint, actor.Region, actor.Mirror);
        actorBatch.Submit();
    }
}
