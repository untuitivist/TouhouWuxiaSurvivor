using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void TestRenderInvalidation()
    {
        StartRun(HeroKind.Reimu, 910);
        canvas._Process(0.1);
        run!.TogglePause();
        canvas._Process(0.1);
        var pausedFrames = canvas.FrameBuildCount;
        canvas._Process(0.1);
        Require(canvas.FrameBuildCount == pausedFrames, "Paused component views are retained without rebuilding");
        run.TogglePause();
        run.Projectiles.Add(new() { Hostile = true, Position = run.PlayerPosition, Life = 1, Damage = 1000, Radius = 5 });
        run.Step(default);
        Require(run.Phase == RunPhase.Lost, "Terminal render fixture reached defeat");
        canvas._Process(0.1);
        Require(canvas.FrameBuildCount > pausedFrames, "Terminal state refreshes the final battle view before freezing");
        var terminalFrames = canvas.FrameBuildCount;
        canvas._Process(0.1);
        Require(canvas.FrameBuildCount == terminalFrames, "Finished battle does not rebuild unchanged batches");
        ShowTitle();
        Godot.GD.Print("RENDER_CACHE_PASS: pause and final-state invalidation");
    }
}
