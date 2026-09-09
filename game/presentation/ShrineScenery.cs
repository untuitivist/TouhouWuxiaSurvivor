using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private readonly List<(ActorArtwork Art, Vector2 Position, float Scale)> sceneryActors = [];

    private void BuildTerrainBatches()
    {
        var terrain = new SpriteBatch(GD.Load<Texture2D>(BaseArt + "scenery/courtyard_tiles.png"));
        worldLayer.AddChild(terrain);
        for (var column = -17; column < 17; column++)
        for (var row = -15; row < 15; row++)
        {
            var position = new Vector2(column * 96 + 48, row * 80 + 40);
            var hash = TileHash(column, row);
            var atEdge = Math.Abs(position.X) > 1260 || Math.Abs(position.Y) > 920;
            var variant = atEdge ? 2 + (int)(hash % 2) : (int)(hash % 2);
            terrain.AddRegion(position, new(96, 80), Colors.White, new(variant * 0.25f, 0, 0.25f, 1));
        }
        terrain.Submit();
        AddScenery("shrine_main", new(0, -1110), 3.2f);
        AddScenery("storehouse", new(-1110, -1120), 2.8f);
        AddScenery("subsidiary_shrine", new(1250, -1110), 2.2f);
        AddScenery("torii", new(0, 1030), 2.1f);
        AddScenery("subsidiary_shrine", new(-630, -380), 1.65f);
        foreach (var position in new Vector2[] { new(-610, 60), new(620, -80), new(-600, 490), new(650, 520), new(-580, -550), new(640, -580) })
            AddScenery("tree_canopy_a", position, 1.15f);
        for (var index = 0; index < 44; index++)
        {
            var hash = TileHash(index, 7);
            var right = index % 2 == 0;
            var position = new Vector2((right ? 1 : -1) * (1080 + hash % 540), -1040 + index / 2 * 102);
            AddScenery(index % 3 == 0 ? "tree_canopy_b" : "tree_canopy_a", position, 1.05f + hash % 4 * 0.15f);
        }
        foreach (var position in new Vector2[] { new(-400, -300), new(400, -300), new(-400, 350), new(400, 350), new(-240, 990), new(240, 990) })
            AddScenery("lantern", position, 1.4f);
    }

    private void AddScenery(string name, Vector2 position, float scale)
        => sceneryActors.Add((actorAtlas["scenery/" + name], position, scale));

    private void QueueSceneryActors()
    {
        foreach (var scenery in sceneryActors)
        {
            var tint = Colors.White;
            if (Run != null)
            {
                var player = Palette.Vector(Run.PlayerPosition);
                var size = scenery.Art.FrameSize * scenery.Scale;
                if (player.Y < scenery.Position.Y && player.Y > scenery.Position.Y - size.Y * WorldProjection.UprightScale
                    && Math.Abs(player.X - scenery.Position.X) < size.X * 0.4f) tint.A = 0.42f;
            }
            QueueActor(scenery.Art, scenery.Position, scenery.Scale, tint);
        }
    }
}
