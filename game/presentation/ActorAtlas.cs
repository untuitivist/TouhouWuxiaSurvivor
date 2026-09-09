using Godot;

namespace Rebirth.Presentation;

public sealed class ActorAtlas
{
    private readonly Dictionary<string, ActorArtwork> entries = [];
    public Texture2D Texture { get; }

    public ActorAtlas()
    {
        Texture = GD.Load<Texture2D>(VisualAssets.Root + "atlas/actors.png");
        var document = Json.ParseString(Godot.FileAccess.GetFileAsString(VisualAssets.Root + "atlas/actors.json")).AsGodotDictionary();
        foreach (var item in document["sprites"].AsGodotArray())
        {
            var entry = item.AsGodotDictionary();
            var name = entry["name"].AsString();
            entries.Add(name, new(new(entry["x"].AsSingle(), entry["y"].AsSingle()),
                new(entry["frame_width"].AsSingle(), entry["frame_height"].AsSingle()),
                entry["columns"].AsInt32(), entry["rows"].AsInt32(), entry["foot_anchor"].AsSingle(), Texture.GetSize()));
        }
    }

    public ActorArtwork this[string name] => entries[name];
}
