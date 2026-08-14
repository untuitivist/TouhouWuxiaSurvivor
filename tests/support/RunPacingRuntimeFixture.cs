using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Completion;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 为节奏流程测试组合真实Boss事件、完成覆盖层、模态输入与可控时钟，并集中负责资源清理。
/// </summary>
public sealed class RunPacingRuntimeFixture
{
    private double _elapsedSeconds = RunPacingTimeline.FinalEncounterSeconds;
    public RunCompletionOverlay Overlay { get; }
    public EcsCombatWorld World { get; }
    public BossEncounterDirector Director { get; }
    public WorldMapOverlay Map { get; }
    public PauseMenuOverlay Pause { get; }
    public CharacterStatsOverlay Stats { get; }
    public RunProgressionCoordinator Progression { get; }
    public RunPacingCoordinator Pacing { get; }
    public RunEndReason? FinalizedReason { get; private set; }

    /// <summary>在指定测试父节点中建立完成层，并以本体默认角色配置独立Boss导演。</summary>
    public RunPacingRuntimeFixture(Node parent)
    {
        Overlay = GD.Load<PackedScene>(
            "res://src/ui/completion/RunCompletionOverlay.tscn")
            .Instantiate<RunCompletionOverlay>();
        parent.AddChild(Overlay);
        World = new EcsCombatWorld();
        Director = new BossEncounterDirector();
        CharacterDefinition selected = CharacterCatalog.Default;
        Director.Configure(World, new RunContentContext(
            ContentPackSelection.BaseOnly, new CharacterSelection(selected)), () => Vector2.Zero);
        Map = new WorldMapOverlay();
        Pause = new PauseMenuOverlay();
        Stats = new CharacterStatsOverlay();
        Progression = new RunProgressionCoordinator();
        Pacing = new RunPacingCoordinator(
            Director, Overlay, Map, Pause, Stats, Progression,
            () => _elapsedSeconds,
            () => FinalizedReason is not null,
            reason =>
            {
                FinalizedReason = reason;
                return true;
            });
    }

    /// <summary>在指定时刻生成并击破合法Boss，把正式事件顺序交给节奏协调器处理。</summary>
    public void DefeatBoss(double elapsedSeconds)
    {
        _elapsedSeconds = elapsedSeconds;
        if (!Director.TrySpawn(new Vector2(200.0f, 0.0f), elapsedSeconds, 0))
        {
            throw new InvalidOperationException("Test boss could not enter the encounter director.");
        }

        World.DamageEnemies(new Vector2(200.0f, 0.0f), 32.0f, 100000);
        if (World.AliveBossCount != 0)
        {
            throw new InvalidOperationException("Test damage did not defeat the spawned boss.");
        }
    }

    /// <summary>断开协调器事件、恢复暂停并安全释放全部测试节点，防止状态污染后续场景。</summary>
    public async Task FreeAsync(Node owner)
    {
        owner.GetTree().Paused = false;
        Pacing.Dispose();
        Director._ExitTree();
        Director.Free();
        World.Free();
        Map.Free();
        Pause.Free();
        Stats.Free();
        Progression.Free();
        Overlay.QueueFree();
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
