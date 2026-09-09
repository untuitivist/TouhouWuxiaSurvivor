using Godot;
using Rebirth.Core;
using Rebirth.Diagnostics;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private async Task TestSpriteBatchRendering()
    {
        var surfaces = new List<SubViewport>();
        try
        {
            StartRun(OS.GetCmdlineUserArgs().Contains("--rebirth-batch-marisa") ? HeroKind.Marisa : HeroKind.Reimu, 42);
            await TestBatchColorState();
            await TestActorAtlasRendering();
            var texture = GD.Load<Texture2D>(VisualAssets.Root + "actors/wild_fairy.png");
            var frames = texture.GetWidth() / texture.GetHeight();
            var actual = new SubViewport { Size = new(512, 512), TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
            var expected = new SubViewport { Size = new(512, 512), TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
            surfaces.Add(actual);
            surfaces.Add(expected);
            AddChild(actual);
            AddChild(expected);
            var batch = new SpriteBatch(texture);
            actual.AddChild(batch);
            var sprites = new List<Sprite2D>();
            for (var index = 0; index < 257; index++)
            {
                var sprite = new Sprite2D { Texture = texture, Hframes = frames, TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Visible = false };
                expected.AddChild(sprite);
                sprites.Add(sprite);
            }
            var counts = new[] { 1, 31, 32, 33, 63, 64, 65, 127, 128, 129, 257, 129, 65, 33, 1, 0, 1, 257, 0, 32, 64, 128, 256, 257 };
            var checks = 0;
            for (var cycle = 0; cycle < 3; cycle++)
            foreach (var count in counts)
            {
                for (var tick = 0; tick < 240; tick++)
                {
                    RunPilot.ResolveChoices(run!);
                    run!.Step(RunPilot.Input(run, run.Ticks));
                    if (run.Phase is RunPhase.Won or RunPhase.Lost) break;
                }
                canvas.Clock = run!.Time;
                canvas.ReceiveEvents();
                if (displayedPhase != run.Phase) RefreshRunScreen();
                batch.Begin();
                for (var index = 0; index < sprites.Count; index++)
                {
                    var sprite = sprites[index];
                    sprite.Visible = index < count;
                    if (!sprite.Visible) continue;
                    var position = new Vector2(16 + index % 20 * 24, 16 + index / 20 * 32);
                    var tint = index % 3 == 0 ? new Color(1.8f, 1.8f, 1.8f) : index % 3 == 1 ? new Color(0.6f, 0.8f, 1, 0.7f) : Colors.White;
                    var frame = (index + checks) % frames;
                    sprite.Position = position;
                    sprite.Scale = Vector2.One * 24 / texture.GetHeight();
                    sprite.Modulate = tint;
                    sprite.Frame = frame;
                    var direction = new Vector2(index % 7 - 3, index % 5 - 2);
                    sprite.Rotation = cycle == 1 ? MathF.Atan2(direction.Y, direction.X) + MathF.PI / 2 : 0;
                    if (cycle == 1) batch.AddDirected(position, Vector2.One * 24, tint, direction, frame, frames);
                    else batch.Add(position, Vector2.One * 24, tint, frame: frame, frameCount: frames);
                }
                batch.Submit();
                if (count == 0) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var actualImage = actual.GetTexture().GetImage();
                using var expectedImage = expected.GetTexture().GetImage();
                var mismatches = CompareRenderedImages(actualImage, expectedImage, 7, out var covered);
                Require(count == 0 || covered > count * 20, "Reference sprites must contain visible pixels");
                GD.Print($"SPRITE_BATCH_CHECK cycle={cycle} count={count} covered={covered} mismatches={mismatches}");
                if (mismatches > Math.Max(4, covered / 100))
                {
                    if (!OS.HasFeature("web"))
                    {
                        SaveBatchImages(actualImage, expectedImage, "sprite");
                    }
                    throw new InvalidOperationException($"Batch differs from ordinary sprites: cycle={cycle} count={count} mismatches={mismatches}/{covered}");
                }
                checks++;
                if (checks % 6 == 0) await CompareBattleBatchRendering(checks);
            }
            batch.Hide();
            var fixedTexture = GD.Load<Texture2D>(VisualAssets.Root + "combat/red_pellet.png");
            var fixedBatch = new SpriteBatch(fixedTexture, 24);
            actual.AddChild(fixedBatch);
            foreach (var count in new[] { 0, 1, 32, 33, 65, 257, 0, 1, 257 })
            {
                fixedBatch.Begin();
                for (var index = 0; index < sprites.Count; index++)
                {
                    var sprite = sprites[index];
                    sprite.Visible = index < count;
                    if (!sprite.Visible) continue;
                    sprite.Position = new(16 + index % 20 * 24, 16 + index / 20 * 32);
                    sprite.Rotation = 0;
                    sprite.Texture = fixedTexture;
                    sprite.Hframes = 1;
                    sprite.Scale = Vector2.One * 24 / fixedTexture.GetHeight();
                    sprite.Modulate = Colors.White;
                    sprite.Frame = 0;
                    fixedBatch.AddPosition(sprite.Position.X, sprite.Position.Y);
                }
                fixedBatch.Submit();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var actualImage = actual.GetTexture().GetImage();
                using var expectedImage = expected.GetTexture().GetImage();
                var mismatches = CompareRenderedImages(actualImage, expectedImage, 7, out var covered);
                Require(count == 0 || covered > count * 20, "Fixed batch reference must remain visible");
                Require(mismatches <= Math.Max(4, covered / 100), "Position-only batches retain size, color and lifecycle state");
                checks++;
            }
            GD.Print($"SPRITE_BATCH_VISUAL_PASS hero={run!.Hero} checks={checks} time={run.Time} phase={run.Phase}");
            if (!OS.HasFeature("web")) GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError($"SPRITE_BATCH_VISUAL_FAIL {error}");
            if (!OS.HasFeature("web")) GetTree().Quit(1);
        }
        finally
        {
            foreach (var surface in surfaces) surface.QueueFree();
        }
    }

    private async Task CompareBattleBatchRendering(int check)
    {
        var originalMode = ProcessMode;
        var originals = Descendants(canvas).OfType<SpriteBatch>().Where(batch => batch.IsVisibleInTree()).ToArray();
        var references = new List<CanvasPass>();
        ProcessMode = ProcessModeEnum.Disabled;
        try
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var actual = GetViewport().GetTexture().GetImage();
            foreach (var batch in originals)
            {
                var reference = batch.CreateRenderReference();
                batch.GetParent().AddChild(reference);
                batch.Hide();
                references.Add(reference);
            }
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var expected = GetViewport().GetTexture().GetImage();
            var mismatches = CompareRenderedImages(actual, expected, 11, out _);
            GD.Print($"BATTLE_BATCH_CHECK check={check} time={run!.Time} batches={originals.Length} mismatches={mismatches}");
            if (check == 36 && !OS.HasFeature("web"))
            {
                SaveBatchImages(actual, expected, $"{run.Hero}-144");
            }
            if (mismatches > 100)
            {
                foreach (var batch in originals) batch.PrintRenderState();
                if (!OS.HasFeature("web"))
                {
                    SaveBatchImages(actual, expected, "battle");
                }
                throw new InvalidOperationException($"Battle batch image differs from independent immediate draws: {mismatches} pixels");
            }
        }
        finally
        {
            foreach (var reference in references) { reference.Hide(); reference.QueueFree(); }
            foreach (var batch in originals) batch.Show();
            ProcessMode = originalMode;
        }
    }

    private static void SaveBatchImages(Image actual, Image expected, string name)
    {
        var output = OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.StartsWith("--rebirth-batch-output=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? "artifacts/batch-validation";
        Directory.CreateDirectory(output);
        if (actual.SavePng(Path.Combine(output, name + "-actual.png")) != Error.Ok || expected.SavePng(Path.Combine(output, name + "-expected.png")) != Error.Ok)
            throw new IOException("Could not save batch render evidence");
    }

    private static int CompareRenderedImages(Image actual, Image expected, int tolerance, out int covered)
    {
        if (actual.GetSize() != expected.GetSize()) throw new InvalidOperationException("Render reference dimensions differ");
        actual.Convert(Image.Format.Rgba8);
        expected.Convert(Image.Format.Rgba8);
        ReadOnlySpan<byte> found = actual.GetData();
        ReadOnlySpan<byte> wanted = expected.GetData();
        var mismatches = 0;
        covered = 0;
        for (var offset = 0; offset < found.Length; offset += 4)
        {
            if (wanted[offset + 3] > 13) covered++;
            if (Math.Abs(found[offset + 3] - wanted[offset + 3]) > tolerance || (Math.Max(found[offset + 3], wanted[offset + 3]) > 13 &&
                (Math.Abs(found[offset] - wanted[offset]) > tolerance || Math.Abs(found[offset + 1] - wanted[offset + 1]) > tolerance || Math.Abs(found[offset + 2] - wanted[offset + 2]) > tolerance))) mismatches++;
        }
        return mismatches;
    }
}

public partial class SpriteBatch
{
    internal void PrintRenderState()
    {
        GD.Print($"BATCH_STATE texture={Texture.ResourcePath} count={Count} bounds={Multimesh.GetAabb()} visible={Multimesh.VisibleInstanceCount}");
        for (var index = 0; index < Math.Min(Count, 3); index++)
        {
            var offset = index * 16;
            GD.Print($"BATCH_INSTANCE {index} cpu=({buffer[offset + 3]},{buffer[offset + 7]}) native={Multimesh.GetInstanceTransform2D(index)} color={Multimesh.GetInstanceColor(index)} frame={Multimesh.GetInstanceCustomData(index)}");
        }
    }

    internal CanvasPass CreateRenderReference()
    {
        var snapshot = (float[])buffer.Clone();
        var count = Count;
        var texture = Texture;
        return new CanvasPass
        {
            Transform = Transform, ZIndex = ZIndex, TextureFilter = TextureFilter,
            Paint = target =>
            {
                for (var index = 0; index < count; index++)
                {
                    var offset = index * 16;
                    var mirror = snapshot[offset + 14] < 0 ? -1 : 1;
                    target.DrawSetTransformMatrix(new(new(snapshot[offset] * mirror, snapshot[offset + 4] * mirror), new(snapshot[offset + 1], snapshot[offset + 5]), new(snapshot[offset + 3], snapshot[offset + 7])));
                    var source = new Rect2(new Vector2(snapshot[offset + 12], snapshot[offset + 13]) * texture.GetSize(), new Vector2(snapshot[offset + 14], snapshot[offset + 15]) * texture.GetSize()).Abs();
                    target.DrawTextureRectRegion(texture, new(-0.5f, -0.5f, 1, 1), source,
                        new(snapshot[offset + 8], snapshot[offset + 9], snapshot[offset + 10], snapshot[offset + 11]));
                }
            }
        };
    }
}
