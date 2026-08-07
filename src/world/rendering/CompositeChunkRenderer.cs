using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 将多个互不依赖的区块渲染器组合为流送器使用的单一入口。
/// </summary>
public sealed class CompositeChunkRenderer : IChunkRenderer
{
    private readonly IReadOnlyList<IChunkRenderer> _renderers;

    /// <summary>
    /// 保存按层级排列的渲染器；至少需要一个渲染器才能形成有效世界视觉。
    /// </summary>
    public CompositeChunkRenderer(params IChunkRenderer[] renderers)
    {
        if (renderers.Length == 0)
        {
            throw new ArgumentException("At least one chunk renderer is required.", nameof(renderers));
        }

        _renderers = renderers;
    }

    /// <summary>
    /// 按注册顺序绘制基础地砖、地区纹理和结构实体。
    /// </summary>
    public void Draw(GeneratedChunk chunk, ChunkCoordinate originChunk)
    {
        foreach (IChunkRenderer renderer in _renderers)
        {
            renderer.Draw(chunk, originChunk);
        }
    }

    /// <summary>
    /// 把同一卸载请求传递给全部渲染层，避免结构精灵滞留在已离开的区块。
    /// </summary>
    public void Erase(ChunkCoordinate absoluteChunk, ChunkCoordinate originChunk)
    {
        foreach (IChunkRenderer renderer in _renderers)
        {
            renderer.Erase(absoluteChunk, originChunk);
        }
    }

    /// <summary>
    /// 清空全部渲染层，使原点重定位后的坐标重新从一致状态开始。
    /// </summary>
    public void Clear()
    {
        foreach (IChunkRenderer renderer in _renderers)
        {
            renderer.Clear();
        }
    }
}
