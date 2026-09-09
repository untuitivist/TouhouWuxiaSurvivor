using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private readonly Node2D worldLayer = new();
    private readonly List<CanvasPass> animatedLayers = [];
    private readonly Dictionary<string, SpriteBatch> batches = [];
    private CanvasPass sceneryLayer = null!;
    private CanvasPass hudLayer = null!;
    private Node2D? activeSurface;
    private Node2D surface => activeSurface ?? this;
    private RunState? renderedRun;
    private RunPhase renderedPhase;
    private bool renderDirty = true;
    private float hudAge;
    public double BatchBuildMilliseconds { get; private set; }
    public int VisibleBatchInstances { get; private set; }
    internal int FrameBuildCount { get; private set; }

    private void InitializeRenderLayers()
    {
        AddChild(worldLayer);
        worldLayer.Scale = new(1, WorldProjection.DepthScale);
        BuildTerrainBatches();
        sceneryLayer = AddPass(worldLayer, 1, DrawScenery);
        animatedLayers.Add(AddPass(worldLayer, 2, DrawWorldDynamic));
        animatedLayers.Add(AddPass(worldLayer, 4, DrawReimuField));
        var beamLayer = AddPass(worldLayer, 4, DrawMarisaBeam);
        beamLayer.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        animatedLayers.Add(beamLayer);
        animatedLayers.Add(AddPass(worldLayer, 5, DrawEnemies));
        InitializeActorBatch();
        animatedLayers.Add(AddPass(worldLayer, 8, DrawPlayer));
        animatedLayers.Add(AddPass(worldLayer, 10, DrawEffects));
        hudLayer = AddPass(this, 11, DrawHud);
        foreach (var name in new[] { "red_pellet", "violet_pellet", "ofuda", "star", "stardust", "dream", "experience", "healing" })
        {
            var fixedSize = name.EndsWith("pellet") ? 15 : name == "experience" ? 12 : name == "healing" ? 17 : 0;
            var batch = new SpriteBatch(GD.Load<Texture2D>($"{BaseArt}combat/{name}.png"), fixedSize) { ZIndex = name.EndsWith("pellet") ? 9 : name is "experience" or "healing" ? 3 : 7 };
            worldLayer.AddChild(batch);
            batches.Add(name, batch);
        }
        var orbBatch = new SpriteBatch(GD.Load<Texture2D>(BaseArt + "actors/yin_yang_orb.png")) { ZIndex = 7 };
        worldLayer.AddChild(orbBatch);
        batches.Add("actors/yin_yang_orb", orbBatch);
        CacheCombatStyles();
        worldLayer.Hide();
        hudLayer.Hide();
    }

    private CanvasPass AddPass(Node parent, int order, Action paint)
    {
        var pass = new CanvasPass { ZIndex = order, Paint = target =>
        {
            activeSurface = target;
            try { if (Run != null) paint(); }
            finally { activeSurface = null; }
        } };
        parent.AddChild(pass);
        return pass;
    }

    private void UpdateRenderLayers(float elapsed)
    {
        var changedRun = renderedRun != Run;
        if (changedRun)
        {
            renderedRun = Run;
            heroAnimation.Reset();
            renderDirty = true;
            sceneryLayer.QueueRedraw();
            QueueRedraw();
        }
        worldLayer.Visible = hudLayer.Visible = Run != null;
        if (Run == null) { QueueRedraw(); return; }
        var offset = new Vector2(640, 370) - new Vector2(camera.X, camera.Y * WorldProjection.DepthScale);
        if (!ReducedMotion) offset += new Vector2(MathF.Sin(Clock * 71), MathF.Cos(Clock * 57)) * shake;
        worldLayer.Position = offset;
        var changedPhase = renderedPhase != Run.Phase;
        if (!renderDirty && !changedPhase && Run.Phase != RunPhase.Playing) return;
        renderDirty = false;
        renderedPhase = Run.Phase;
        FrameBuildCount++;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        UpdateCombatBatches();
        BatchBuildMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        foreach (var layer in animatedLayers) layer.QueueRedraw();
        hudAge += elapsed;
        if (changedRun || changedPhase || hudAge >= 0.1f) { hudAge = 0; hudLayer.QueueRedraw(); }
    }

}
