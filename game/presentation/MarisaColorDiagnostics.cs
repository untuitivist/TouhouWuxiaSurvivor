using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private async Task TestMarisaColors()
    {
        var actual = new SubViewport { Size = new(704, 176), TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
        var expected = new SubViewport { Size = actual.Size, TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
        AddChild(actual);
        AddChild(expected);
        try
        {
            var texture = GD.Load<Texture2D>("res://assets/internal_original/base/combat/star_variants.png");
            Require(texture.GetWidth() == GameCanvas.StarColorFrames * 16 && texture.GetHeight() == 16, "Original star atlas dimensions");
            var batch = new SpriteBatch(texture);
            actual.AddChild(batch);
            for (var frame = 0; frame < GameCanvas.StarColorVariants; frame++)
            {
                var position = new Vector2(28 + frame * 49, 88);
                batch.Add(position, Vector2.One * 40, Colors.White, frame: frame + 1, frameCount: GameCanvas.StarColorFrames);
                expected.AddChild(new Sprite2D { Texture = texture, Hframes = GameCanvas.StarColorFrames, Frame = frame + 1, Position = position, Scale = Vector2.One * 2.5f, TextureFilter = CanvasItem.TextureFilterEnum.Nearest });
            }
            batch.Submit();
            await DrawTwice();
            using (var atlasImage = actual.GetTexture().GetImage())
            using (var reference = expected.GetTexture().GetImage())
            {
                var mismatches = CompareRenderedImages(atlasImage, reference, 7, out var covered);
                GD.Print($"MARISA_STAR_COLORS_CHECK covered={covered} mismatches={mismatches}");
                if (!OS.HasFeature("web") && (covered <= 5000 || mismatches > 20)) SaveBatchImages(atlasImage, reference, "original-star-colors");
                Require(covered > 5000 && mismatches <= 20, "All original star frames match independent sprites");
                Require(ColorFamilies(atlasImage) >= 6, "Original stars retain distinct source colors");
                GD.Print($"MARISA_STAR_COLORS_PASS frames={GameCanvas.StarColorVariants} mismatches={mismatches}");
            }
            batch.Hide();
            var beamTexture = GD.Load<Texture2D>("res://assets/internal_original/base/effects/master_spark.png");
            var material = GameCanvas.CreateSparkMaterial();
            var beam = new CanvasPass { Material = material, Paint = target =>
            {
                target.DrawTextureRectRegion(beamTexture, new(32, 24, 128, 128), new(0, 0, 128, 128), Colors.White);
                target.DrawTextureRectRegion(beamTexture, new(160, 24, 512, 128), new(128, 0, 128, 128), Colors.White);
            } };
            actual.AddChild(beam);
            GameCanvas.ConfigureSparkMaterial(material, 0, 640, 128, false);
            await DrawTwice();
            using var initial = actual.GetTexture().GetImage();
            Require(ColorFamilies(initial) >= 6, "A single beam contains multiple simultaneous colors");
            GameCanvas.ConfigureSparkMaterial(material, 0.35f, 640, 128, false);
            await DrawTwice();
            using var flowing = actual.GetTexture().GetImage();
            var changed = CompareRenderedImages(initial, flowing, 7, out _);
            Require(changed > 10000, "Rainbow flows spatially through the original beam texture");
            var initialPixels = initial.GetData();
            var flowingPixels = flowing.GetData();
            for (var offset = 3; offset < initialPixels.Length; offset += 4)
                Require(Math.Abs(initialPixels[offset] - flowingPixels[offset]) <= 1, "Flow preserves original transparency");
            await DrawTwice();
            using var frozen = actual.GetTexture().GetImage();
            Require(flowing.GetData().SequenceEqual(frozen.GetData()), "Unchanged gameplay clock freezes beam flow");
            GameCanvas.ConfigureSparkMaterial(material, 1, 640, 128, true);
            await DrawTwice();
            using var reduced = actual.GetTexture().GetImage();
            GameCanvas.ConfigureSparkMaterial(material, 9, 640, 128, true);
            await DrawTwice();
            using var reducedLater = actual.GetTexture().GetImage();
            Require(reduced.GetData().SequenceEqual(reducedLater.GetData()) && ColorFamilies(reduced) >= 6, "Reduced motion preserves a static rainbow");
            GD.Print($"MARISA_FLOWING_SPARK_PASS families={ColorFamilies(initial)} changedPixels={changed} alpha=preserved pause=frozen reduced=static-rainbow");
        }
        finally
        {
            actual.QueueFree();
            expected.QueueFree();
        }
    }

    private async Task DrawTwice()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private static int ColorFamilies(Image image)
    {
        image.Convert(Image.Format.Rgba8);
        var pixels = image.GetData();
        var width = image.GetWidth();
        var height = image.GetHeight();
        var families = new HashSet<int>();
        for (var vertical = 0; vertical < height; vertical += 2)
        for (var horizontal = 0; horizontal < width; horizontal += 2)
        {
            var offset = (vertical * width + horizontal) * 4;
            var red = pixels[offset];
            var green = pixels[offset + 1];
            var blue = pixels[offset + 2];
            var maximum = Math.Max(red, Math.Max(green, blue));
            var minimum = Math.Min(red, Math.Min(green, blue));
            if (pixels[offset + 3] < 26 || maximum < 51 || maximum - minimum < 39) continue;
            var threshold = maximum * 0.75f;
            families.Add((red > threshold ? 1 : 0) | (green > threshold ? 2 : 0) | (blue > threshold ? 4 : 0));
        }
        return families.Count;
    }
}
