using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Balance;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Scaling;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 把符卡定义转换为具体战斗实体与伤害，同时保持运行协调器不依赖效果细节。
/// </summary>
public sealed class SpellCardEffectCaster : ISpellCardEffectExecutor
{
    private readonly Node2D _player;
    private readonly PlayerHealth _health;
    private readonly SpellCardCombatBackend _backend;
    private readonly Node2D _effects;
    private readonly PackedScene _orbScene;
    private readonly PackedScene _circleScene;
    private readonly ISpellCardAttributeProvider _attributes;
    private readonly RunBuildState? _build;

    /// <summary>
    /// 注入施法者、生命、敌人和效果容器，以及两类可实例化视觉场景。
    /// </summary>
    public SpellCardEffectCaster(
        Node2D player,
        PlayerHealth health,
        Node2D enemies,
        Node2D effects,
        PackedScene orbScene,
        PackedScene circleScene,
        ISpellCardAttributeProvider attributes,
        EcsCombatWorld? ecsWorld = null,
        RunBuildState? build = null)
    {
        _player = player;
        _health = health;
        _backend = new SpellCardCombatBackend(enemies, ecsWorld);
        _effects = effects;
        _orbScene = orbScene;
        _circleScene = circleScene;
        _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        _build = build;
    }

    /// <summary>
    /// 使用当前角色与构筑属性解析一次不可变战斗参数，确保下一次施展会自然响应升级。
    /// </summary>
    public ResolvedSpellCardCombat Resolve(SpellCardDefinition card)
    {
        ResolvedSpellCardCombat resolved = SpellCardScalingResolver.Resolve(
            card.Combat, _attributes.Capture());
        int rank = _build?.GetRank(card.UnlockUpgradeId) ?? 1;
        return SpellCardMasteryScaler.Apply(resolved, rank);
    }

    /// <summary>
    /// 按效果类型选择攻势载体，再把选敌、起手与轨迹完整委托给卡牌声明的几何策略。
    /// </summary>
    public bool TryCast(
        SpellCardDefinition card,
        ResolvedSpellCardCombat resolved) => card.EffectKind switch
    {
        SpellCardEffectKind.HomingVolley => CastTargetedVolley(card, resolved, false),
        SpellCardEffectKind.FocusedVolley => CastTargetedVolley(card, resolved, true),
        SpellCardEffectKind.AreaBurst => CastArea(card, resolved, false),
        SpellCardEffectKind.GuardField => CastArea(card, resolved, true),
        _ => false,
    };

    /// <summary>
    /// 为射程内目标生成按属性缩放的灵玉；集中型重复首目标，追踪型依次分配不同目标。
    /// </summary>
    private bool CastTargetedVolley(
        SpellCardDefinition card,
        ResolvedSpellCardCombat resolved,
        bool focused)
    {
        IReadOnlyList<SpellCardTargetReference> candidates = focused
            ? _backend.SelectHighestThreatTargets(
                _player.GlobalPosition, resolved.EffectRange)
            : _backend.SelectCandidateTargets(
                _player.GlobalPosition, resolved.EffectRange);
        SpellCardGeometryPlan plan = CreatePlan(card, resolved, focused,
            candidates.Select(target => target.InitialPosition).ToArray());
        if (plan.Projectiles.Count == 0)
        {
            return false;
        }

        var assignedTargets = focused
            ? null
            : new HashSet<SpellCardTargetReference>();
        for (int index = 0; index < plan.Projectiles.Count; index++)
        {
            SpellCardTrajectory trajectory = plan.Projectiles[index];
            var orb = _orbScene.Instantiate<FantasySealOrb>();
            orb.ConfigureImpact(
                _backend,
                trajectory.TargetPosition,
                resolved.Damage,
                resolved.ProjectileSpeed,
                resolved.ImpactRange, resolved.TravelDurationSeconds, index,
                card.SourcePackId, card.FullName, card.GeometryKind, trajectory.Curvature,
                SpellCardCombatBackend.MatchTarget(
                    trajectory.TargetPosition, candidates, assignedTargets));
            _effects.AddChild(orb);
            orb.GlobalPosition = trajectory.SpawnPosition;
        }

        return true;
    }

    /// <summary>
    /// 对结界内最近目标结算距离衰减伤害、延长玩家无敌并生成一次结界演出。
    /// </summary>
    private bool CastArea(
        SpellCardDefinition card,
        ResolvedSpellCardCombat resolved,
        bool grantDefense)
    {
        SpellCardGeometryPlan plan = CreatePlan(card, resolved, false,
            _backend.SelectCandidates(_player.GlobalPosition, resolved.EffectRange));
        _backend.DamageImpacts(
            _player.GlobalPosition,
            plan,
            resolved.Damage,
            resolved.EffectRange,
            (float)SpellCardContributionModel.AreaEdgeDamageMultiplier);

        if (grantDefense && resolved.DefenseSeconds > 0.0f)
        {
            _health.GrantInvincibility(resolved.DefenseSeconds);
        }

        var effect = _circleScene.Instantiate<SealingCircleEffect>();
        effect.Configure(card.SourcePackId, card.FullName, card.GeometryKind);
        _effects.AddChild(effect);
        effect.GlobalPosition = plan.VisualCenter;
        return true;
    }

    /// <summary>使用卡牌声明的策略规划本次命中目标和轨迹，数值预算在进入策略前已经固定。</summary>
    private SpellCardGeometryPlan CreatePlan(
        SpellCardDefinition card,
        ResolvedSpellCardCombat resolved,
        bool focused,
        IReadOnlyList<Vector2> candidates)
    {
        var request = new SpellCardGeometryRequest(
            _player.GlobalPosition,
            candidates,
            resolved.TargetCount,
            resolved.EffectRange,
            resolved.SpawnDistance,
            focused);
        return SpellCardGeometryCatalog.Get(card.GeometryKind).CreatePlan(request);
    }

}
