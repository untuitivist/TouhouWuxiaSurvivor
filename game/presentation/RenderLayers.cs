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
        BuildTerrainBatches();
        sceneryLayer = AddPass(worldLayer, 1, DrawScenery);
        animatedLayers.Add(AddPass(worldLayer, 2, DrawWorldDynamic));
        animatedLayers.Add(AddPass(worldLayer, 4, DrawReimuField));
        var beamLayer = AddPass(worldLayer, 4, DrawMarisaBeam);
        beamLayer.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        animatedLayers.Add(beamLayer);
        animatedLayers.Add(AddPass(worldLayer, 6, DrawEnemies));
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
        foreach (var name in new[] { "actors/kedama", "actors/wild_fairy", "actors/mountain_spirit", "actors/great_youkai", "actors/yin_yang_orb" })
        {
            var batch = new SpriteBatch(textures[name]) { ZIndex = 5 };
            worldLayer.AddChild(batch);
            batches.Add(name, batch);
        }
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
            renderDirty = true;
            sceneryLayer.QueueRedraw();
            QueueRedraw();
        }
        worldLayer.Visible = hudLayer.Visible = Run != null;
        if (Run == null) { QueueRedraw(); return; }
        var offset = new Vector2(640, 370) - camera;
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

    private void BuildTerrainBatches()
    {
        var grass = new SpriteBatch(GD.Load<Texture2D>($"{BaseArt}combat/grass.png"));
        var path = new SpriteBatch(textures["path"]);
        worldLayer.AddChild(grass);
        worldLayer.AddChild(path);
        for (var column = -34; column <= 33; column++)
        for (var row = -26; row <= 25; row++)
        {
            var location = new Vector2(column * 48, row * 48);
            var hash = TileHash(column, row);
            var outside = Math.Abs(location.X) > RunState.ArenaHalfWidth || Math.Abs(location.Y) > RunState.ArenaHalfHeight;
            var isPath = Math.Abs(location.X + 24) < 75 || Math.Abs(location.Y + 24) < 70 || Math.Abs(location.X - location.Y * 1.45f) < 48;
            var tint = outside ? new Color("101f26") : isPath ? new Color("526563") : new Color("879f89");
            var brightness = 0.88f + hash % 5 * 0.025f;
            (isPath ? path : grass).Add(location + new Vector2(24, 24), new(48, 48), tint * new Color(brightness, brightness, brightness));
        }
        grass.Submit();
        path.Submit();
    }
}
