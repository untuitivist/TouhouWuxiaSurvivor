using Godot;
using Rebirth.Core;
using NumericsVector = System.Numerics.Vector2;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private bool performancePreview;
    private double performanceSeconds;
    private readonly List<double> renderFrameSamples = [];
    private readonly List<double> renderCallSamples = [];

    private void PreparePerformancePreview()
    {
        performancePreview = true;
        StartRun(HeroKind.Reimu, 71);
        run!.Enemies.Clear();
        run.Projectiles.Clear();
        Array.Clear(run.Ranks);
        for (var index = 0; index < 320; index++)
            run.Enemies.Add(new() { Id = index + 1, Kind = (EnemyKind)(index % 4), Position = new(index % 20 * 60 - 580, index / 20 * 40 - 300), Radius = 14, Health = 1000, MaxHealth = 1000 });
        for (var index = 0; index < 1600; index++)
            run.Projectiles.Add(new() { Position = new(index % 50 * 24 - 600, index / 50 * 20 - 320), Velocity = NumericsVector.UnitY, Life = 60, Radius = 5,
                Hostile = index % 4 < 2, Alternate = index % 4 == 1, Art = index % 4 == 2 ? ArtKind.Ofuda : ArtKind.Stars });
        for (var index = 0; index < 400; index++) run.Pickups.Add(new() { Position = new(index % 40 * 29 - 580, index / 40 * 62 - 280), Value = 1, Healing = index % 16 == 0 });
        canvas.ResetView();
        RefreshRunScreen();
    }

    private void RecordRenderFrame(double delta)
    {
        if (!performancePreview || diagnosticFinished) return;
        performanceSeconds += delta;
        if (performanceSeconds < 1) return;
        renderFrameSamples.Add(delta * 1000);
        renderCallSamples.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
    }

    private void SaveRenderReport(string screenshot)
    {
        if (!performancePreview) return;
        var report = new Godot.Collections.Dictionary
        {
            ["driver"] = RenderingServer.GetVideoAdapterName(), ["renderer"] = RenderingServer.GetCurrentRenderingMethod(),
            ["frames"] = renderFrameSamples.Count, ["frameMs"] = renderFrameSamples.Average(),
            ["drawCalls"] = renderCallSamples.Average(),
            ["enemies"] = run!.Enemies.Count, ["projectiles"] = run.Projectiles.Count, ["pickups"] = run.Pickups.Count,
            ["window"] = DisplayServer.WindowGetSize().ToString(), ["renderTarget"] = GetViewport().GetTexture().GetSize().ToString(),
            ["scope"] = "Static deterministic render fixture; desktop capture is not mobile FPS."
        };
        var json = Json.Stringify(report, "  ");
        File.WriteAllText(Path.ChangeExtension(screenshot, ".json"), json, new System.Text.UTF8Encoding(false));
        GD.Print("RENDER_BENCHMARK " + json);
    }
}
