namespace TouhouWuxiaSurvivor.Ecs.Core;

/// <summary>
/// 表示 ECS 世界中的轻量实体句柄；它不承载行为，只用于索引组件数据。
/// </summary>
public readonly struct EcsEntity : IEquatable<EcsEntity>
{
    /// <summary>
    /// 创建一个稳定的整数实体句柄；零被保留为无效句柄。
    /// </summary>
    public EcsEntity(int value) => Value = value;

    /// <summary>
    /// 获取实体在对应数据池中的标识。
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// 判断句柄是否指向一个可以继续使用的实体。
    /// </summary>
    public bool IsValid => Value > 0;

    /// <summary>
    /// 比较两个实体句柄，便于系统记录来源或目标实体。
    /// </summary>
    public bool Equals(EcsEntity other) => Value == other.Value;

    /// <summary>
    /// 将句柄与普通对象比较，遵循值类型相等语义。
    /// </summary>
    public override bool Equals(object? obj) => obj is EcsEntity other && Equals(other);

    /// <summary>
    /// 返回实体句柄的整数哈希，允许作为字典键使用。
    /// </summary>
    public override int GetHashCode() => Value;
}
