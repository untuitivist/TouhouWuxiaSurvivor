namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 声明项目全部基础地表 Tile 的分类、名称、三色调色板、图案与固定种子。
/// </summary>
internal static class TileCatalog
{
    /// <summary>
    /// 返回按运行时 TileId 顺序维护的完整 Tile 规格列表。
    /// </summary>
    public static IReadOnlyList<TileSpec> Create()
    {
        return
        [
            T("common", "grass_base", 0x477A45, 0x5C914F, 0x2F5A39, PatternKind.Speckles, 101),
            T("common", "grass_dots_01", 0x477A45, 0x659957, 0x315D3B, PatternKind.DenseSpeckles, 102),
            T("common", "dirt_base", 0x8A6847, 0xA27B52, 0x644A38, PatternKind.Speckles, 103),
            T("common", "dirt_pebbles_01", 0x8A6847, 0xB08A60, 0x5D4434, PatternKind.Pebbles, 104),
            T("common", "stone_base", 0x6F7778, 0x8A9291, 0x50585B, PatternKind.Speckles, 105),
            T("common", "stone_cracks_01", 0x6F7778, 0x929A98, 0x414A4E, PatternKind.Cracks, 106),
            T("common", "water_shallow_base", 0x3F7791, 0x5A9DB0, 0x2D5A78, PatternKind.Ripples, 107),
            T("common", "water_shallow_ripples_01", 0x427F99, 0x6BAEBB, 0x2B5878, PatternKind.Ripples, 108),

            T("hakurei_shrine", "shrine_grass_base", 0x587A4A, 0x75955A, 0x3D5B3D, PatternKind.Speckles, 201),
            T("hakurei_shrine", "shrine_grass_petals_01", 0x587A4A, 0x78985E, 0xE9A7B6, PatternKind.Petals, 202),
            T("hakurei_shrine", "shrine_path_base", 0xB09A7C, 0xCAB493, 0x806D59, PatternKind.Speckles, 203),
            T("hakurei_shrine", "shrine_path_pebbles_01", 0xB09A7C, 0xD0B995, 0x776452, PatternKind.Pebbles, 204),
            T("hakurei_shrine", "boundary_soil_base", 0x78614D, 0x92755C, 0x4E4050, PatternKind.Speckles, 205),
            T("hakurei_shrine", "boundary_soil_sparkles_01", 0x78614D, 0x9B7B5E, 0xE4D0F0, PatternKind.Sparkles, 206),

            T("magic_forest", "forest_floor_base", 0x304C39, 0x476044, 0x22362D, PatternKind.Speckles, 301),
            T("magic_forest", "forest_floor_leaves_01", 0x304C39, 0x5B6C42, 0x88915A, PatternKind.Leaves, 302),
            T("magic_forest", "moss_base", 0x456B42, 0x62864E, 0x2D5035, PatternKind.Speckles, 303),
            T("magic_forest", "moss_dots_01", 0x456B42, 0x71945A, 0x2D5035, PatternKind.DenseSpeckles, 304),
            T("magic_forest", "magic_soil_base", 0x4A3D52, 0x65516B, 0x302A3C, PatternKind.Speckles, 305),
            T("magic_forest", "magic_soil_sparkles_01", 0x4A3D52, 0x735B78, 0xD7B4E5, PatternKind.Sparkles, 306),

            T("misty_lake", "lake_water_base", 0x477F9E, 0x70B7C4, 0x315D80, PatternKind.Ripples, 401),
            T("misty_lake", "lake_water_ripples_01", 0x477F9E, 0x84C6CE, 0x315D80, PatternKind.Ripples, 402),
            T("misty_lake", "lake_shore_base", 0x9A8766, 0xB5A078, 0x6B5B4B, PatternKind.Speckles, 403),
            T("misty_lake", "lake_shore_pebbles_01", 0x9A8766, 0xC1AA80, 0x645648, PatternKind.Pebbles, 404),
            T("misty_lake", "wet_grass_base", 0x467063, 0x5E8B73, 0x2D504C, PatternKind.Speckles, 405),
            T("misty_lake", "wet_grass_droplets_01", 0x467063, 0x638F78, 0x8DC5D0, PatternKind.Droplets, 406),

            T("bamboo_forest", "bamboo_floor_base", 0x6C7041, 0x8A8950, 0x4B5134, PatternKind.Speckles, 501),
            T("bamboo_forest", "bamboo_floor_leaves_01", 0x6C7041, 0x99945A, 0xB2A65C, PatternKind.Leaves, 502),
            T("bamboo_forest", "bamboo_moss_base", 0x4C7142, 0x688B50, 0x335436, PatternKind.Speckles, 503),
            T("bamboo_forest", "bamboo_moss_dots_01", 0x4C7142, 0x76975B, 0x335436, PatternKind.DenseSpeckles, 504),
            T("bamboo_forest", "bamboo_path_base", 0x8B7650, 0xA68D5D, 0x62543E, PatternKind.Speckles, 505),
            T("bamboo_forest", "bamboo_path_stripes_01", 0x8B7650, 0xA68D5D, 0x5B7041, PatternKind.Stripes, 506),

            T("youkai_mountain", "mountain_grass_base", 0x5B7648, 0x76905B, 0x3C583A, PatternKind.Speckles, 601),
            T("youkai_mountain", "mountain_grass_flowers_01", 0x5B7648, 0x7D9860, 0xE5D28A, PatternKind.Flowers, 602),
            T("youkai_mountain", "mountain_rock_base", 0x687477, 0x818D8D, 0x485356, PatternKind.Speckles, 603),
            T("youkai_mountain", "mountain_rock_cracks_01", 0x687477, 0x8D9897, 0x3D494E, PatternKind.Cracks, 604),
            T("youkai_mountain", "stream_stone_base", 0x63797E, 0x7E9698, 0x465C63, PatternKind.Pebbles, 605),
            T("youkai_mountain", "stream_stone_wet_01", 0x63797E, 0x8DA6A5, 0x83B8C5, PatternKind.WetStones, 606),
        ];
    }

    /// <summary>
    /// 将紧凑的 RGB 整数参数转换为一个完整 TileSpec，减少目录表的重复语法。
    /// </summary>
    private static TileSpec T(
        string category,
        string id,
        uint baseColor,
        uint accentA,
        uint accentB,
        PatternKind pattern,
        uint seed)
    {
        return new TileSpec(
            category,
            id,
            FromRgb(baseColor),
            FromRgb(accentA),
            FromRgb(accentB),
            pattern,
            seed);
    }

    /// <summary>
    /// 将 0xRRGGBB 整数拆分为不透明 RGBA32 颜色。
    /// </summary>
    private static Rgba32 FromRgb(uint value)
    {
        return new Rgba32(
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value);
    }
}
