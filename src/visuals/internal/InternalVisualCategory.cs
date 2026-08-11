namespace TouhouWuxiaSurvivor.Visuals.Internal;

/// <summary>
/// 标识内部素材清单中的内容分类，使图鉴和正式玩法能够共享映射而不互相依赖 UI 类型。
/// </summary>
public enum InternalVisualCategory
{
    Biome,
    Structure,
    Enemy,
    Character,
    SpellCard,
    Pickup,
}
