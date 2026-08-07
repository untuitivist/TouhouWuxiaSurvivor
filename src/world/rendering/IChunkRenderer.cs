using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 定义无限世界区块的绘制、卸载和重定位清理契约，使地砖、地区素材与结构可以独立渲染。
/// </summary>
public interface IChunkRenderer
{
    /// <summary>
    /// 将一个绝对区块投影到当前本地原点附近。
    /// </summary>
    void Draw(GeneratedChunk chunk, ChunkCoordinate originChunk);

    /// <summary>
    /// 卸载指定绝对区块在当前本地原点下创建的全部视觉。
    /// </summary>
    void Erase(ChunkCoordinate absoluteChunk, ChunkCoordinate originChunk);

    /// <summary>
    /// 清空全部本地视觉，供无限世界原点重定位后重新投影。
    /// </summary>
    void Clear();
}
