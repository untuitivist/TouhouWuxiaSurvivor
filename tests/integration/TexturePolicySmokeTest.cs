using Godot;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证像素游戏的全局最近邻、像素对齐和无 mipmap 策略，并审计全部运行时场景与 PNG。
/// </summary>
public partial class TexturePolicySmokeTest : Node
{
    /// <summary>
    /// 检查项目设置，递归实例化 src 场景检查 CanvasItem，再加载 assets PNG 检查 mipmap。
    /// </summary>
    public override void _Ready()
    {
        Require(ProjectSettings.GetSetting(
            "rendering/textures/canvas_textures/default_texture_filter").AsInt64() == 0,
            "Global canvas texture filter must be nearest.");
        Require(ProjectSettings.GetSetting(
            "rendering/2d/snap/snap_2d_transforms_to_pixel").AsBool(),
            "2D transforms must snap to pixels.");
        Require(ProjectSettings.GetSetting(
            "rendering/2d/snap/snap_2d_vertices_to_pixel").AsBool(),
            "2D vertices must snap to pixels.");

        foreach (string scenePath in FindResources("res://src", ".tscn"))
        {
            VerifyScene(scenePath);
        }

        foreach (string texturePath in FindResources("res://assets", ".png"))
        {
            VerifyTexture(texturePath);
        }

        GD.Print("Texture policy smoke test passed.");
        GetTree().Quit();
    }

    /// <summary>
    /// 实例化一个运行时场景并递归确认所有 CanvasItem 只继承全局过滤或显式使用最近邻。
    /// </summary>
    private void VerifyScene(string scenePath)
    {
        PackedScene scene = GD.Load<PackedScene>(scenePath);
        Node instance = scene.Instantiate();
        AddChild(instance);
        VerifyCanvasItems(instance, scenePath);
        RemoveChild(instance);
        instance.Free();
    }

    /// <summary>
    /// 递归检查节点树，拒绝 Linear、LinearMipmap 等会使像素边缘变软的过滤模式。
    /// </summary>
    private static void VerifyCanvasItems(Node node, string scenePath)
    {
        if (node is CanvasItem canvasItem)
        {
            Require(canvasItem.TextureFilter is
                CanvasItem.TextureFilterEnum.ParentNode or CanvasItem.TextureFilterEnum.Nearest,
                $"Linear texture filter found at {scenePath}:{node.GetPath()}.");
        }

        foreach (Node child in node.GetChildren())
        {
            VerifyCanvasItems(child, scenePath);
        }
    }

    /// <summary>
    /// 加载 PNG 资源并确认导入图像没有 mipmap，避免缩放时采样到混色层级。
    /// </summary>
    private static void VerifyTexture(string texturePath)
    {
        Texture2D texture = GD.Load<Texture2D>(texturePath);
        Image image = texture.GetImage();
        Require(!image.HasMipmaps(), $"Mipmaps must be disabled: {texturePath}.");
    }

    /// <summary>
    /// 递归枚举指定资源目录下具有目标扩展名的文件，并返回稳定排序的 res:// 路径。
    /// </summary>
    private static IReadOnlyList<string> FindResources(string root, string extension)
    {
        var results = new List<string>();
        CollectResources(root, extension, results);
        results.Sort(StringComparer.Ordinal);
        return results;
    }

    /// <summary>
    /// 深度遍历 Godot 资源目录，把匹配扩展名的普通文件加入结果集合。
    /// </summary>
    private static void CollectResources(string directoryPath, string extension, List<string> results)
    {
        using DirAccess directory = DirAccess.Open(directoryPath);
        directory.ListDirBegin();
        string name = directory.GetNext();
        while (!string.IsNullOrEmpty(name))
        {
            string path = $"{directoryPath}/{name}";
            if (directory.CurrentIsDir())
            {
                CollectResources(path, extension, results);
            }
            else if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(path);
            }

            name = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    /// <summary>
    /// 将策略违规转换为明确异常，使无头 Godot 测试返回失败状态。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
