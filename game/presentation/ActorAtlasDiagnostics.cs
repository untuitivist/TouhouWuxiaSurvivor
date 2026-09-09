using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private async Task TestActorAtlasRendering()
    {
        var atlas = new ActorAtlas();
        var actual = new SubViewport { Size = new(400, 300), TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
        var expected = new SubViewport { Size = actual.Size, TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, World2D = new() };
        AddChild(actual);
        AddChild(expected);
        try
        {
            var batch = new SpriteBatch(atlas.Texture) { Scale = new(1, WorldProjection.DepthScale) };
            actual.AddChild(batch);
            var parent = new Node2D { Scale = batch.Scale };
            expected.AddChild(parent);
            var names = new[] { "players/reimu", "players/marisa", "actors/yin_yang_orb", "actors/wild_fairy" };
            var references = names.Select(_ => new Sprite2D { Texture = atlas.Texture, RegionEnabled = true, TextureFilter = CanvasItem.TextureFilterEnum.Nearest }).ToArray();
            foreach (var sprite in references) parent.AddChild(sprite);
            for (var check = 0; check < 32; check++)
            {
                batch.Begin();
                for (var index = 0; index < names.Length; index++)
                {
                    var art = atlas[names[index]];
                    var region = art.Region(check % 8, check / 8);
                    var foot = new Vector2(120 + index * 34, 210 + index * 6);
                    var size = art.FrameSize * 1.5f * new Vector2(1, WorldProjection.UprightScale);
                    var position = foot + new Vector2(0, size.Y * 0.5f - art.FootAnchor * 1.5f * WorldProjection.UprightScale);
                    var mirror = false;
                    batch.AddRegion(position, size, Colors.White, region, mirror);
                    var sprite = references[index];
                    sprite.RegionRect = new(region.Position * atlas.Texture.GetSize(), region.Size * atlas.Texture.GetSize());
                    sprite.Position = position;
                    sprite.Scale = size / art.FrameSize;
                    sprite.FlipH = mirror;
                }
                batch.Submit();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var actualImage = actual.GetTexture().GetImage();
                using var expectedImage = expected.GetTexture().GetImage();
                var mismatches = CompareRenderedImages(actualImage, expectedImage, 7, out var covered);
                Require(covered > 1500 && mismatches < 30, $"Directional atlas/projection mismatch: {check}, {mismatches}/{covered}");
            }
            GD.Print("ACTOR_ATLAS_VISUAL_PASS checks=32 rows=4 mirror=false overlap=true upright=true");
        }
        finally
        {
            actual.QueueFree();
            expected.QueueFree();
        }
    }
}
