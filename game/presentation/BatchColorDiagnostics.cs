using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private async Task TestBatchColorState()
    {
        var actual = new SubViewport { Size = new(256, 256), TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
        var expected = new SubViewport { Size = actual.Size, TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
        AddChild(actual);
        AddChild(expected);
        try
        {
            var texture = GD.Load<Texture2D>(VisualAssets.Root + "actors/wild_fairy.png");
            var frames = texture.GetWidth() / texture.GetHeight();
            var colors = new[] { Colors.Black, new Color(0.2f, 0.8f, 0.4f), new Color(1, 0.1f, 0.3f, 0.4f), Colors.White };
            var predecessors = new List<CanvasPass>();
            var batches = new List<SpriteBatch>();
            var sprites = new List<Sprite2D>();
            var predecessorColor = Colors.Black;
            var vertexColors = false;
            for (var index = 0; index < 3; index++)
            {
                var offset = index * 80;
                foreach (var viewport in new[] { actual, expected })
                {
                    var pass = new CanvasPass { Paint = target =>
                    {
                        Vector2[] points = [new(4, offset + 4), new(20, offset + 4), new(20, offset + 20), new(4, offset + 20)];
                        if (vertexColors) target.DrawPolygon(points, [predecessorColor, predecessorColor, predecessorColor, predecessorColor]);
                        else target.DrawColoredPolygon(points, predecessorColor);
                    } };
                    viewport.AddChild(pass);
                    predecessors.Add(pass);
                }
                var modulation = index == 1 ? new Color(0.8f, 1, 0.7f, 0.8f) : Colors.White;
                var batch = new SpriteBatch(texture) { Modulate = modulation };
                actual.AddChild(batch);
                batches.Add(batch);
                for (var column = 0; column < 3; column++)
                {
                    var sprite = new Sprite2D { Texture = texture, Hframes = frames, TextureFilter = CanvasItem.TextureFilterEnum.Nearest, SelfModulate = modulation };
                    expected.AddChild(sprite);
                    sprites.Add(sprite);
                }
            }
            for (var check = 0; check < 24; check++)
            {
                predecessorColor = colors[check % colors.Length];
                vertexColors = check % 8 >= 4;
                foreach (var predecessor in predecessors) predecessor.QueueRedraw();
                for (var row = 0; row < batches.Count; row++)
                {
                    var batch = batches[row];
                    batch.Begin();
                    for (var column = 0; column < 3; column++)
                    {
                        var sprite = sprites[row * 3 + column];
                        sprite.Visible = check % 12 != 8;
                        if (!sprite.Visible) continue;
                        sprite.Position = new(48 + column * 80, 40 + row * 80);
                        sprite.Scale = Vector2.One * 48 / texture.GetHeight();
                        sprite.Rotation = check % 3 * MathF.PI / 2;
                        sprite.Modulate = column == 0 ? Colors.White : column == 1 ? new Color(0.6f, 0.8f, 1, 0.7f) : new Color(1.8f, 1.8f, 1.8f);
                        sprite.Frame = check % frames;
                        batch.Add(sprite.Position, Vector2.One * 48, sprite.Modulate, sprite.Rotation, sprite.Frame, frames);
                    }
                    batch.Submit();
                }
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var actualImage = actual.GetTexture().GetImage();
                using var expectedImage = expected.GetTexture().GetImage();
                var mismatches = CompareRenderedImages(actualImage, expectedImage, 7, out var covered);
                Require(covered > (check % 12 == 8 ? 500 : 4000), "Color reference must contain visible pixels");
                GD.Print($"BATCH_COLOR_CHECK check={check} vertexColors={vertexColors} predecessor={predecessorColor} covered={covered} mismatches={mismatches}");
                if (mismatches > 20)
                {
                    if (!OS.HasFeature("web")) SaveBatchImages(actualImage, expectedImage, "color-state");
                    throw new InvalidOperationException($"Batch color depends on preceding polygon: check={check} mismatches={mismatches}");
                }
            }
            GD.Print("BATCH_COLOR_VISUAL_PASS checks=24");
        }
        finally
        {
            actual.QueueFree();
            expected.QueueFree();
        }
    }
}
