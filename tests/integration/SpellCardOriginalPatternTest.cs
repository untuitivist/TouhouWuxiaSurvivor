using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Ecs.Core;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 锁定本体与红魔乡逐卡原作依据、复合弹型和 Boss 时序，防止批量迁移退化为通用追踪弹。
/// </summary>
public partial class SpellCardOriginalPatternTest : Node
{
    private const string ScarletPackId = "th06_eosd";

    private static readonly IReadOnlyDictionary<string,
        (string Reference, SpellCardPatternKind Pattern,
            BossProjectilePatternKind BossPattern,
            SpellBulletStyleKind PrimaryStyle, int AccentCount)> VerifiedPatterns =
        new Dictionary<string,
            (string Reference, SpellCardPatternKind Pattern,
                BossProjectilePatternKind BossPattern,
                SpellBulletStyleKind PrimaryStyle, int AccentCount)>(StringComparer.Ordinal)
        {
            ["reimu_fantasy_seal"] = new("TH06 Reimu-A Bomb",
                SpellCardPatternKind.HomingOrbit, BossProjectilePatternKind.HomingOrbit,
                SpellBulletStyleKind.Orb, 0),
            ["reimu_evil_sealing_circle"] = new("TH06 Reimu-B Bomb",
                SpellCardPatternKind.SealPulse, BossProjectilePatternKind.SealPulse,
                SpellBulletStyleKind.Amulet, 0),
            ["reimu_duplex_barrier"] = new("TH08 SC055/056",
                SpellCardPatternKind.SealPulse, BossProjectilePatternKind.SealPulse,
                SpellBulletStyleKind.Amulet, 0),
            ["reimu_omnidirectional_oni_binding_circle"] = new("TH08 SC065",
                SpellCardPatternKind.SealPulse, BossProjectilePatternKind.SealPulse,
                SpellBulletStyleKind.Amulet, 0),
            ["marisa_master_spark"] = new("TH08 SC090/091",
                SpellCardPatternKind.StraightBeam, BossProjectilePatternKind.StraightBeam,
                SpellBulletStyleKind.Laser, 0),
            ["marisa_stardust_reverie"] = new("TH08 SC082/083",
                SpellCardPatternKind.StardustFan, BossProjectilePatternKind.StardustFan,
                SpellBulletStyleKind.Star, 0),
            ["th06_rumia_night_bird"] = new("TH06 SC02",
                SpellCardPatternKind.AimedArc, BossProjectilePatternKind.AimedArc,
                SpellBulletStyleKind.Orb, 0),
            ["th06_cirno_perfect_freeze"] = new("TH06 SC06",
                SpellCardPatternKind.FreezeRelease, BossProjectilePatternKind.FreezeRelease,
                SpellBulletStyleKind.Orb, 1),
            ["th06_meiling_rainbow_wind_chime"] = new("TH06 SC10",
                SpellCardPatternKind.RotatingStream, BossProjectilePatternKind.RotatingStream,
                SpellBulletStyleKind.Needle, 1),
            ["th06_patchouli_philosophers_stone"] = new("TH06 SC54",
                SpellCardPatternKind.ElementalCycle, BossProjectilePatternKind.ElementalCycle,
                SpellBulletStyleKind.Orb, 4),
            ["th06_sakuya_killing_doll"] = new("TH06 SC40",
                SpellCardPatternKind.TimeStopRedirect, BossProjectilePatternKind.TimeStopRedirect,
                SpellBulletStyleKind.Knife, 0),
            ["th06_remilia_scarlet_shoot"] = new("TH06 SC45",
                SpellCardPatternKind.AimedTrail, BossProjectilePatternKind.AimedTrail,
                SpellBulletStyleKind.LargeOrb, 1),
            ["th06_flandre_laevatein"] = new("TH06 SC56",
                SpellCardPatternKind.SweepingBeam, BossProjectilePatternKind.SweepingBeam,
                SpellBulletStyleKind.Laser, 1),
        };

    /// <summary>执行资料、发射、运动与素材契约，并将任一退化转换为明确测试退出码。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyReferenceCoverage();
            VerifyDefinitions();
            VerifyBossExecutions();
            VerifyCompositeAtlasStyles();
            VerifyMotionTransitions();
            GD.Print("Spell-card original pattern test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认所有声明 Boss 符卡能力的包都完成逐卡资料，而迁移库存保持明确未校对状态。</summary>
    private static void VerifyReferenceCoverage()
    {
        string[] requiredSources = ContentPackCatalog.Installed
            .Where(pack => pack.HasCapability(ContentPackCapabilityIds.BossSpellSequences))
            .Select(pack => pack.Id).ToArray();
        SpellCardDefinition[] required = SpellCardCatalog.All.Where(card =>
            requiredSources.Contains(card.SourcePackId, StringComparer.Ordinal)).ToArray();
        Require(required.Length == VerifiedPatterns.Count && required.All(card =>
                card.Pattern.IsOriginalReferenceVerified &&
                card.Pattern.ReferenceUrl.StartsWith("https://", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(card.Pattern.OriginalBehavior)),
            "A boss-sequence content pack contains an unverified spell pattern.");
        Require(SpellCardCatalog.All.Where(card =>
                !requiredSources.Contains(card.SourcePackId, StringComparer.Ordinal))
            .All(card => card.Pattern.Kind == SpellCardPatternKind.LegacyGeometry),
            "An inventory pack silently claimed an unverified original sequence.");
    }

    /// <summary>逐张核对本体与红魔乡编号、时序、主弹型与辅弹型数量。</summary>
    private static void VerifyDefinitions()
    {
        foreach ((string id, var expected) in VerifiedPatterns)
        {
            SpellCardDefinition card = SpellCardCatalog.FindById(id)
                ?? throw new InvalidOperationException($"Missing Scarlet spell: {id}");
            Require(card.Pattern.OriginalReference == expected.Reference &&
                card.Pattern.Kind == expected.Pattern &&
                card.BulletStyleKind == expected.PrimaryStyle &&
                card.Pattern.AccentBulletStyles.Count == expected.AccentCount,
                $"Scarlet spell semantics drifted: {id}.");
        }
    }

    /// <summary>通过正式解析器和发射器确认七张卡进入不同 ECS 语法，并携带必要运动状态。</summary>
    private static void VerifyBossExecutions()
    {
        foreach ((string id, var expected) in VerifiedPatterns)
        {
            SpellCardDefinition card = SpellCardCatalog.FindById(id)!;
            BossAttackPattern attack = BossSpellAttackPatternFactory.Create(card);
            Require(attack.PatternKind == expected.BossPattern,
                $"Boss resolver lost the original pattern: {id}.");
            EnemyComponent enemy = CreateBoss(card);
            var shots = new List<EnemyProjectileSpawnRequest>();
            BossSpellProjectileEmitter.Emit(ref enemy, new Vector2(140.0f, 48.0f),
                attack, 1, request => { shots.Add(request); return true; });
            Require(shots.Count > 0, $"Original boss pattern emitted no bullets: {id}.");
            VerifyMotionRequest(id, shots);
        }
    }

    /// <summary>按代表卡检查冻结、旋流和时停转向状态，并确认复合弹型真的进入请求。</summary>
    private static void VerifyMotionRequest(
        string id,
        IReadOnlyList<EnemyProjectileSpawnRequest> shots)
    {
        if (id.Contains("perfect_freeze", StringComparison.Ordinal))
            Require(shots.Any(shot => shot.Motion.Kind == ProjectileMotionKind.FreezeResume) &&
                shots.Any(shot => shot.Motion.Kind == ProjectileMotionKind.Linear),
                "Perfect Freeze lost its frozen spread or aimed blue bullets.");
        if (id.Contains("rainbow_wind_chime", StringComparison.Ordinal))
            Require(shots.Any(shot => shot.Motion.Kind == ProjectileMotionKind.CurvedStream),
                "Rainbow Wind Chime emitted no rotating stream.");
        if (id.Contains("killing_doll", StringComparison.Ordinal))
            Require(shots.Any(shot => shot.Motion.Kind == ProjectileMotionKind.RedirectOnce),
                "Killing Doll emitted no time-stop redirect.");
        if (id.Contains("philosophers_stone", StringComparison.Ordinal))
            Require(shots.Select(shot => shot.VisualBulletStyleId).Distinct().Count() >= 4,
                "Philosopher's Stone did not rotate elemental bullet styles.");
        if (id.Contains("scarlet_shoot", StringComparison.Ordinal))
            Require(shots.Select(shot => shot.VisualBulletStyleId).Distinct().Count() == 2,
                "Scarlet Shoot did not combine main and trailing bullets.");
    }

    /// <summary>以正式视觉目录逐个解析主辅弹型，保证复合演出没有跨内容包借图。</summary>
    private static void VerifyCompositeAtlasStyles()
    {
        var visualCatalog = new InternalVisualCatalog();
        var resolver = new SpellCardProjectileVisualResolver();
        resolver.Configure(visualCatalog);
        foreach (SpellCardDefinition card in SpellCardCatalog.All.Where(card =>
                     card.SourcePackId is "base" or ScarletPackId))
        {
            int binding = SpellCardVisualBindingCatalog.GetBindingId(card.Id);
            SpellBulletStyleKind[] styles = new[] { card.BulletStyleKind }
                .Concat(card.Pattern.AccentBulletStyles).ToArray();
            Require(styles.All(style => resolver.TryResolve(binding, style, 0,
                    out Texture2D _, out SpellBulletVisualSelection _)),
                $"Composite spell style cannot use its own atlas: {card.Id}.");
        }
    }

    /// <summary>直接推进纯运动策略，确认冻结窗口无位移且时停结束只执行一次定向。</summary>
    private static void VerifyMotionTransitions()
    {
        Vector2 velocity = Vector2.Right * 100.0f;
        float age = 0.0f;
        bool applied = false;
        var freeze = new ProjectileMotionProfile(
            ProjectileMotionKind.FreezeResume, 0.1f, 0.2f, TurnAngle: 0.3f);
        Require(ProjectileMotionPolicy.Step(ref velocity, ref age, ref applied,
                Vector2.Zero, freeze, 0.1f) &&
            !ProjectileMotionPolicy.Step(ref velocity, ref age, ref applied,
                Vector2.Zero, freeze, 0.1f),
            "Freeze motion did not enter its hold window.");
        Require(ProjectileMotionPolicy.Step(ref velocity, ref age, ref applied,
                Vector2.Zero, freeze, 0.15f) && applied,
            "Freeze motion did not resume after its hold window.");

        velocity = Vector2.Right * 100.0f;
        age = 0.0f;
        applied = false;
        var redirect = new ProjectileMotionProfile(
            ProjectileMotionKind.RedirectOnce, 0.1f, 0.1f,
            RedirectTarget: new Vector2(0.0f, 80.0f));
        _ = ProjectileMotionPolicy.Step(ref velocity, ref age, ref applied,
            Vector2.Zero, redirect, 0.2f);
        Require(applied && velocity.Y > 0.0f && Mathf.IsZeroApprox(velocity.X),
            "Time-stop redirect did not face its stored target.");
    }

    /// <summary>为纯发射测试建立一名真实角色 Boss，不进入场景树或共享世界。</summary>
    private static EnemyComponent CreateBoss(SpellCardDefinition card)
    {
        CharacterDefinition character = CharacterCatalog.GetRequired(card.OwnerCharacterId);
        return new EnemyComponent(new EcsEntity(77), Vector2.Zero,
            BossDefinitionFactory.Create(character, card.SourcePackId));
    }

    /// <summary>把原作资料或运行时语义错误转换为带卡号上下文的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
