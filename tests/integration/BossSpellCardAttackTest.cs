using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证本体与红魔乡角色 Boss 的生命强化、所属符卡弹幕、阶段密度和原作图集绑定。
/// </summary>
public partial class BossSpellCardAttackTest : Node
{
    private const string ScarletPackId = "th06_eosd";

    /// <summary>执行角色覆盖、纯数据攻击、ECS 发射和视觉映射断言，并用退出码报告结果。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyCharacterSpellCoverage();
            VerifyBossBalanceAndFallback();
            VerifyPhasePresentation();
            VerifyOriginalBulletVisuals();
            GD.Print("Boss spell-card attack test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认本体两名角色和红魔乡七名角色都至少拥有一张归属自己的运行时符卡。</summary>
    private static void VerifyCharacterSpellCoverage()
    {
        CharacterDefinition[] supported = CharacterCatalog.All.Where(character =>
            character.AvailableSourcePackIds.Contains(ContentPackCatalog.Base.Id,
                StringComparer.Ordinal) ||
            character.AvailableSourcePackIds.Contains(ScarletPackId,
                StringComparer.Ordinal)).ToArray();
        Require(supported.Length == 9,
            $"Expected two base and seven Scarlet characters, found {supported.Length}.");
        foreach (CharacterDefinition character in supported)
        {
            Require(SpellCardCatalog.All.Any(card =>
                    card.OwnerCharacterId == character.CharacterId &&
                    (card.SourcePackId == ContentPackCatalog.Base.Id ||
                        card.SourcePackId == ScarletPackId)),
                $"Supported boss has no owned spell card: {character.DisplayName}");
        }
    }

    /// <summary>确认 Boss 生命统一提高到六倍而接触伤害不变，并让其他作品继续使用通用弹幕。</summary>
    private static void VerifyBossBalanceAndFallback()
    {
        CharacterDefinition rumia = CharacterCatalog.GetRequiredByDisplayName("露米娅");
        Actors.Enemies.EnemyDefinition boss = BossDefinitionFactory.Create(rumia);
        Require(boss.MaxHealth == BossCombatBalancePolicy.ScaleHealth(
                rumia.BossProfile.MaxHealth) &&
            boss.MaxHealth == (int)Math.Ceiling(rumia.BossProfile.MaxHealth * 6.0) &&
            boss.BaseMaxHealth == (int)Math.Ceiling(rumia.BossProfile.MaxHealth) &&
            boss.ContactDamage == (int)Math.Ceiling(rumia.BossProfile.ContactDamage),
            "Boss health or unchanged contact-damage contract regressed.");

        var resolver = new SpellCardBossAttackResolver();
        CharacterDefinition unsupported = CharacterCatalog.All.First(character =>
            character.SourcePackId == "th07_pcb");
        Require(!resolver.TryResolve(unsupported.CharacterId,
                BossBulletPhase.AimedFan, out _),
            "An unfinished work unexpectedly bypassed the generic boss barrage fallback.");
    }

    /// <summary>用露米娅的一张符卡依次触发高、中、低血阶段，确认密度递增且演出身份稳定。</summary>
    private static void VerifyPhasePresentation()
    {
        CharacterDefinition rumia = CharacterCatalog.GetRequiredByDisplayName("露米娅");
        var pool = new EnemyPool();
        pool.Add(new Vector2(96.0f, 0.0f), BossDefinitionFactory.Create(rumia));
        var system = new EnemyProjectileSystem();
        system.ConfigureBossAttacks(new SpellCardBossAttackResolver());

        (int high, int highStyle) = FireAtRatio(pool, system, 0.9f);
        (int middle, int middleStyle) = FireAtRatio(pool, system, 0.5f);
        (int low, int lowStyle) = FireAtRatio(pool, system, 0.2f);
        EnemyComponent boss = pool.Get(0);
        Require(high > 0 && middle > high && low > middle,
            $"Boss spell density did not rise by phase: {high}/{middle}/{low}.");
        Require(highStyle > 0 && highStyle == middleStyle && middleStyle == lowStyle,
            "Boss projectiles lost their stable spell-card visual binding.");
        Require(boss.ActiveSpellName == "Night Bird" && boss.SpellAnnouncementTime > 0.0f,
            "Boss did not expose its active spell name for the world presentation.");
    }

    /// <summary>触发指定生命比例的一波正式 ECS 符卡弹幕，并返回弹数与唯一视觉编号。</summary>
    private static (int Shots, int Style) FireAtRatio(
        EnemyPool pool,
        EnemyProjectileSystem system,
        float healthRatio)
    {
        EnemyComponent boss = pool.Get(0);
        boss.Health = Math.Max(1, (int)(boss.Definition.MaxHealth * healthRatio));
        boss.FireCooldown = 0.0f;
        pool.Set(0, boss);
        var requests = new List<EnemyProjectileSpawnRequest>();
        system.Step(pool, Vector2.Zero, 0.01f,
            request => { requests.Add(request); return true; });
        Require(requests.Count > 0 && requests.All(request =>
            request.VisualStyleId == requests[0].VisualStyleId),
            "One boss volley mixed unrelated spell-card visual bindings.");
        return (requests.Count, requests[0].VisualStyleId);
    }

    /// <summary>逐卡解析本体和红魔乡图集区域，并确认视觉编号能完整写入投射物池。</summary>
    private static void VerifyOriginalBulletVisuals()
    {
        var visuals = new InternalVisualCatalog();
        var resolver = new SpellCardProjectileVisualResolver();
        resolver.Configure(visuals);
        foreach (SpellCardDefinition card in SpellCardCatalog.All.Where(card =>
                     card.SourcePackId is "base" or ScarletPackId))
        {
            int binding = SpellCardVisualBindingCatalog.GetBindingId(card.Id);
            Require(resolver.TryResolve(binding, 3, out Texture2D texture,
                    out SpellBulletVisualSelection selection) &&
                texture.GetWidth() >= 16 && selection.Source.Size.X is 16.0f or 32.0f,
                $"Spell bullet visual is not usable: {card.Id}");
        }

        var pool = new ProjectilePool();
        Require(pool.TryAdd(Vector2.Zero, Vector2.Right, 80.0f, 1,
                ProjectileFaction.Enemy, 2.0f, 3.0f, 0, out _,
                visualStyleId: 7) && pool.Get(0).VisualStyleId == 7,
            "Projectile pool discarded the spell-card visual binding.");
    }

    /// <summary>把 Boss 符卡契约失败转换为带精确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
