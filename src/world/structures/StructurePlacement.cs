namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 表示一个结构在无限世界中的完整确定性实例，身份不受区块加载顺序影响。
/// </summary>
public readonly record struct StructurePlacement
{
    public StructureId Id { get; }
    public long X { get; }
    public long Y { get; }
    public ulong InstanceId { get; }
    public string DefinitionId { get; }
    public int QuarterTurns { get; }
    public int Variant { get; }
    public int FootprintRadius { get; }

    /// <summary>
    /// 保留旧调用的三参数构造方式，并补齐定义、朝向和占地信息。
    /// </summary>
    public StructurePlacement(StructureId id, long x, long y)
        : this(id, x, y, StableLegacyId(id, x, y),
            StructureCatalog.GetRequired(id).DefinitionId, 0, 0,
            StructureCatalog.GetRequired(id).Placement.FootprintRadius)
    {
    }

    /// <summary>
    /// 创建定位器生成的完整实例；朝向限定为四方向，变体限定为非负值。
    /// </summary>
    public StructurePlacement(
        StructureId id,
        long x,
        long y,
        ulong instanceId,
        string definitionId,
        int quarterTurns,
        int variant,
        int footprintRadius)
    {
        Id = id;
        X = x;
        Y = y;
        InstanceId = instanceId;
        DefinitionId = definitionId;
        QuarterTurns = ((quarterTurns % 4) + 4) % 4;
        Variant = Math.Max(0, variant);
        FootprintRadius = Math.Max(1, footprintRadius);
    }

    /// <summary>
    /// 保留旧式三值解构语义，避免地图和调试工具因元数据扩充被迫迁移。
    /// </summary>
    public void Deconstruct(out StructureId id, out long x, out long y) =>
        (id, x, y) = (Id, X, Y);

    /// <summary>
    /// 为手工或测试创建的旧式 placement 生成稳定实例号。
    /// </summary>
    private static ulong StableLegacyId(StructureId id, long x, long y) =>
        Generation.DeterministicHash.At((ulong)(int)id, x, y, 0x7A11UL);
}
