using TouhouWuxiaSurvivor.Tools.TileGenerator;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: tile_generator <output-directory>");
    return 2;
}

string outputRoot = Path.GetFullPath(args[0]);
IReadOnlyList<TileSpec> specs = TileCatalog.Create();
CatalogWriter writer = new();
int generatedCount = writer.Generate(outputRoot, specs);

Console.WriteLine($"Generated {generatedCount} tiles at {outputRoot}");
return 0;
