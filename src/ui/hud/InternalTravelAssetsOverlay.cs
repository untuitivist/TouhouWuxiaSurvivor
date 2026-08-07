using Godot;
using TouhouWuxiaSurvivor.Visuals.Internal;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Streaming;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Ui.Hud;

/// <summary>
/// 在局内显示角色、当前地区和附近结构的内部原作缩略图，并提供可见素材声明。
/// </summary>
public partial class InternalTravelAssetsOverlay : Control
{
    private const string BaseSourceId = "base";
    private readonly InternalVisualCatalog _catalog = new();
    private TextureRect? _portrait;
    private TextureRect? _biomePreview;
    private TextureRect? _structurePreview;
    private Label? _biomeName;
    private Label? _structureName;
    private Label? _notice;
    private WorldGenerator? _generator;
    private ChunkStreamer? _streamer;
    private Node2D? _player;
    private string _lastBiomeKey = string.Empty;
    private string _lastStructureKey = string.Empty;

    /// <summary>
    /// 缓存界面节点，并在内部素材不存在的公开包中整体隐藏，避免留下无效黑框。
    /// </summary>
    public override void _Ready()
    {
        _portrait = GetNode<TextureRect>("Panel/Layout/Portrait");
        _biomePreview = GetNode<TextureRect>("Panel/Layout/BiomePreview");
        _structurePreview = GetNode<TextureRect>("Panel/Layout/StructurePreview");
        _biomeName = GetNode<Label>("Panel/Layout/Text/BiomeName");
        _structureName = GetNode<Label>("Panel/Layout/Text/StructureName");
        _notice = GetNode<Label>("Notice");
        Visible = _catalog.Count > 0;
        if (!Visible)
        {
            return;
        }

        TrySetTexture(_portrait, BaseSourceId, InternalVisualCategory.Character, "博丽灵梦");
        _notice.Text = "内部开发素材引用：东方 Project 原作素材包；仅供内部验证，公开前替换";
    }

    /// <summary>
    /// 注入世界查询来源，使叠层只读取当前状态而不参与地形、结构或存档生成。
    /// </summary>
    public void Configure(WorldGenerator generator, ChunkStreamer streamer, Node2D player)
    {
        _generator = generator;
        _streamer = streamer;
        _player = player;
        Refresh();
    }

    /// <summary>
    /// 仅在地区或最近结构改变时重载缩略图，避免每帧重复资源查询与界面写入。
    /// </summary>
    public void Refresh()
    {
        if (!Visible || _generator is null || _streamer is null || _player is null)
        {
            return;
        }

        (long tileX, long tileY) = GridMath.LocalPositionToAbsoluteTile(
            _player.Position, _streamer.OriginChunk);
        BiomeId biome = _generator.Biomes.Select(tileX, tileY);
        string biomeSource = GetSourceId(biome);
        string biomeName = BiomeNames.GetChinese(biome);
        string biomeKey = biomeSource + ":" + biomeName;
        if (biomeKey != _lastBiomeKey)
        {
            _lastBiomeKey = biomeKey;
            _biomeName!.Text = biomeName;
            TrySetTexture(_biomePreview!, biomeSource, InternalVisualCategory.Biome, biomeName);
        }

        UpdateNearestStructure(tileX, tileY);
    }

    /// <summary>
    /// 查找玩家周围的最近结构锚点，并以同一结构名称与内容包 ID 解析缩略图。
    /// </summary>
    private void UpdateNearestStructure(long tileX, long tileY)
    {
        IReadOnlyList<StructurePlacement> nearby = _generator!.StructureLocations
            .FindInBounds(tileX - 48, tileY - 48, tileX + 48, tileY + 48);
        StructurePlacement? nearest = nearby.Count == 0
            ? null
            : nearby.OrderBy(placement => DistanceSquared(placement, tileX, tileY)).First();
        if (nearest is null)
        {
            SetStructureFallback();
            return;
        }

        string sourceId = GetSourceId(nearest.Value.Id);
        string name = StructureNames.GetChinese(nearest.Value.Id);
        string key = sourceId + ":" + name;
        if (key == _lastStructureKey)
        {
            return;
        }

        _lastStructureKey = key;
        _structureName!.Text = name;
        TrySetTexture(_structurePreview!, sourceId, InternalVisualCategory.Structure, name);
    }

    /// <summary>
    /// 在范围内没有结构时清空缩略图和名称，避免显示已离开的远方地标。
    /// </summary>
    private void SetStructureFallback()
    {
        if (_lastStructureKey == "none")
        {
            return;
        }

        _lastStructureKey = "none";
        _structureName!.Text = "附近无地标";
        _structurePreview!.Texture = null;
    }

    /// <summary>
    /// 按清单键加载指定纹理；缺失映射时清空目标，允许正作内容逐项补充而不显示旧图。
    /// </summary>
    private void TrySetTexture(
        TextureRect target,
        string sourceId,
        InternalVisualCategory category,
        string name)
    {
        target.Texture = _catalog.TryGet(sourceId, category, name, out var definition) &&
            _catalog.TryGetTexture(definition, out Texture2D texture)
            ? texture
            : null;
    }

    /// <summary>
    /// 从正作地区或结构定义取得内容包 ID，本体枚举则统一归入 base。
    /// </summary>
    private static string GetSourceId(BiomeId biome) =>
        OfficialWorldContentCatalog.TryGet(biome, out OfficialWorldContentDefinition definition)
            ? definition.PackId
            : BaseSourceId;

    /// <summary>
    /// 从正作结构定义取得内容包 ID，本体结构则统一归入 base。
    /// </summary>
    private static string GetSourceId(StructureId structure) =>
        OfficialWorldContentCatalog.TryGet(structure, out OfficialWorldContentDefinition definition)
            ? definition.PackId
            : BaseSourceId;

    /// <summary>
    /// 计算结构锚点到玩家绝对 Tile 的平方距离，排序时避免引入平方根。
    /// </summary>
    private static long DistanceSquared(StructurePlacement placement, long tileX, long tileY)
    {
        long dx = placement.X - tileX;
        long dy = placement.Y - tileY;
        return dx * dx + dy * dy;
    }
}
