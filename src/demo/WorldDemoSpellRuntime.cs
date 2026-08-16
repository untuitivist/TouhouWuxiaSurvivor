using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;

namespace TouhouWuxiaSurvivor.Demo;

/// <summary>
/// 在世界组合边界装配奥义属性源、效果执行器和解锁源，避免 WorldDemo 与协调器承担内部构造细节。
/// </summary>
public static class WorldDemoSpellRuntime
{
    /// <summary>使用正式世界组件构造窄依赖，并把它们一次性注入无资源自动奥义协调器。</summary>
    public static void Configure(
        SpellCardCoordinator coordinator,
        SpellCardEffectAssets assets,
        Node2D player,
        PlayerHealth health,
        AutoShooter shooter,
        RunProgressionCoordinator progression,
        PlayableCharacterProfile character,
        Node2D enemies,
        Node2D effects,
        EcsCombatWorld ecsWorld,
        ContentPackSelection content)
    {
        var attributes = new SpellCardAttributeProvider(
            shooter, progression.Modifiers, health, character);
        var executor = new SpellCardEffectCaster(
            player, health, enemies, effects,
            assets.RequireFantasySealOrb(), assets.RequireSealingCircle(),
            attributes, ecsWorld, progression.Build);
        coordinator.Configure(
            executor,
            new RunBuildSpellCardUnlockSource(progression.Build, content),
            new SpellCardTriggerFactory(),
            new WorldSpellCardTriggerEnvironment(player, health, ecsWorld));
    }
}
