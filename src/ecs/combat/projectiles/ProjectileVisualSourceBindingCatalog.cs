using TouhouWuxiaSurvivor.Content;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 把内容包稳定 ID 映射为仅在本进程使用的紧凑整数，避免每颗 ECS 弹丸保存或比较字符串。
/// </summary>
public static class ProjectileVisualSourceBindingCatalog
{
    private static IReadOnlyDictionary<string, int>? _bindings;
    private static IReadOnlyDictionary<int, string>? _sourceIds;

    /// <summary>返回内容包的非零视觉编号；空来源明确表示本体，未知来源立即失败。</summary>
    public static int GetBindingId(string? sourcePackId)
    {
        EnsureLoaded();
        string resolved = sourcePackId ?? ContentPackCatalog.Base.Id;
        return _bindings!.TryGetValue(resolved, out int binding)
            ? binding
            : throw new KeyNotFoundException(
                $"Projectile visual source pack is not installed: {resolved}");
    }

    /// <summary>把运行期编号还原为内容包 ID；零和未知编号返回 false，禁止静默借用本体。</summary>
    public static bool TryGetSourcePackId(int bindingId, out string sourcePackId)
    {
        EnsureLoaded();
        return _sourceIds!.TryGetValue(bindingId, out sourcePackId!);
    }

    /// <summary>按已安装目录顺序一次性建立双向映射，编号只服务当前运行且不进入存档。</summary>
    private static void EnsureLoaded()
    {
        if (_bindings is not null)
        {
            return;
        }

        var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
        var sourceIds = new Dictionary<int, string>();
        for (int index = 0; index < ContentPackCatalog.Installed.Count; index++)
        {
            string sourceId = ContentPackCatalog.Installed[index].Id;
            int binding = index + 1;
            bindings.Add(sourceId, binding);
            sourceIds.Add(binding, sourceId);
        }

        _bindings = bindings;
        _sourceIds = sourceIds;
    }
}
