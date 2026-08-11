namespace TouhouWuxiaSurvivor.Visuals.Internal;

/// <summary>
/// 标识内部素材的版式，使调用者只加载与自身渲染方式兼容的资源。
/// </summary>
public enum InternalVisualKind
{
    Scene,
    ActorStrip,
    Portrait,
    BulletAtlas,
    ItemAtlas,
}
