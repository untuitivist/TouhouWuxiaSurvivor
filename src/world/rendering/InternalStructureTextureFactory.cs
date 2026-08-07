using Godot;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 从原作结构场景提取色彩并生成透明俯视地标纹样，避免把横版背景截图直接覆盖在世界上。
/// </summary>
public static class InternalStructureTextureFactory
{
    /// <summary>
    /// 使用原图平均色、暗部和亮部绘制 128×128 俯视结构，并按结构类型改变占地图形。
    /// </summary>
    public static Texture2D CreateMarker(Texture2D sourceTexture, StructureId structure)
    {
        Image source = sourceTexture.GetImage();
        source.Convert(Image.Format.Rgba8);
        Color average = Sample(source, 0.5f).Darkened(0.3f);
        Color shadow = Sample(source, 0.15f).Darkened(0.18f);
        Color accent = Sample(source, 0.85f).Darkened(0.25f);
        Image marker = Image.CreateEmpty(128, 128, false, Image.Format.Rgba8);
        marker.Fill(Colors.Transparent);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                int layer = GetLayer(structure, x - 64, y - 64);
                if (layer == 0)
                {
                    continue;
                }

                Color color = layer == 3 ? accent : layer == 2 ? shadow : average;
                color.A = layer == 1 ? 0.78f : 0.94f;
                marker.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(marker);
    }

    /// <summary>
    /// 根据已接入的本体与红魔乡结构选择俯视占地图形，其他结构回退为小型殿堂。
    /// </summary>
    private static int GetLayer(StructureId structure, int dx, int dy) => structure switch
    {
        StructureId.HakureiShrine or StructureId.ShrineCourt => ShrineLayer(dx, dy),
        StructureId.HumanVillage => VillageLayer(dx, dy),
        StructureId.MagicCircle => MagicCircleLayer(dx, dy),
        StructureId.LakeIsland => IslandLayer(dx, dy),
        StructureId.ScarletDevilMansion or StructureId.VoileLibrary => MansionLayer(dx, dy),
        StructureId.MountainTerrace => TerraceLayer(dx, dy),
        StructureId.Crossroads => CrossroadsLayer(dx, dy),
        _ => MansionLayer(dx, dy),
    };

    /// <summary>
    /// 绘制北侧本殿、南北参道与鸟居横梁，形成可从俯视地图辨认的博丽神社轮廓。
    /// </summary>
    private static int ShrineLayer(int dx, int dy)
    {
        bool hall = Math.Abs(dx) <= 38 && dy is >= -46 and <= -18;
        bool hallBorder = hall && (Math.Abs(dx) >= 34 || dy is -46 or -18);
        bool path = Math.Abs(dx) <= 7 && dy is > -18 and <= 60;
        bool torii = dy is >= 27 and <= 31 && Math.Abs(dx) <= 26 ||
            Math.Abs(dx) is >= 21 and <= 25 && dy is >= 27 and <= 48;
        return torii ? 3 : hallBorder ? 2 : hall || path ? 1 : 0;
    }

    /// <summary>
    /// 绘制十字主街和四角房舍，人里地标会与底层结构压印的道路方向一致。
    /// </summary>
    private static int VillageLayer(int dx, int dy)
    {
        bool road = Math.Abs(dx) <= 6 || Math.Abs(dy) <= 6;
        bool house = Math.Abs(dx) is >= 18 and <= 46 && Math.Abs(dy) is >= 18 and <= 44;
        bool wall = house && (Math.Abs(dx) is >= 40 or <= 22 || Math.Abs(dy) is >= 38 or <= 22);
        return wall ? 2 : house ? 3 : road && Math.Abs(dx) <= 56 && Math.Abs(dy) <= 56 ? 1 : 0;
    }

    /// <summary>
    /// 绘制双层圆环、四向符线与中心核心，作为魔法阵遗迹的实际地表纹样。
    /// </summary>
    private static int MagicCircleLayer(int dx, int dy)
    {
        int distance = dx * dx + dy * dy;
        bool outerRing = distance is >= 42 * 42 and <= 47 * 47;
        bool innerRing = distance is >= 21 * 21 and <= 25 * 25;
        bool rays = (Math.Abs(dx) <= 2 || Math.Abs(dy) <= 2) && distance <= 42 * 42;
        return outerRing ? 2 : innerRing || rays ? 3 : distance <= 8 * 8 ? 1 : 0;
    }

    /// <summary>
    /// 绘制不规则湖岸、中央陆地和短栈道，使雾湖小岛保持自然边缘而非规则圆标。
    /// </summary>
    private static int IslandLayer(int dx, int dy)
    {
        int shapedDistance = dx * dx + dy * dy + Math.Abs(dx * dy) / 3;
        bool shore = shapedDistance is >= 37 * 37 and <= 46 * 46;
        bool land = shapedDistance < 37 * 37;
        bool dock = dx is >= -3 and <= 3 && dy is >= 28 and <= 58;
        return shore ? 2 : dock ? 3 : land ? 1 : 0;
    }

    /// <summary>
    /// 绘制主馆、左右翼和南侧入口，用于红魔馆与大图书馆的俯视建筑占地。
    /// </summary>
    private static int MansionLayer(int dx, int dy)
    {
        bool center = Math.Abs(dx) <= 28 && dy is >= -38 and <= 20;
        bool wings = Math.Abs(dx) is >= 28 and <= 52 && dy is >= -25 and <= 12;
        bool entrance = Math.Abs(dx) <= 7 && dy is > 20 and <= 57;
        bool wall = (center || wings) &&
            (Math.Abs(dx) is >= 47 and <= 52 || dy is -38 or -25 or 12 or 20);
        return wall ? 2 : center || wings ? 3 : entrance ? 1 : 0;
    }

    /// <summary>
    /// 绘制多层水平山阶与两侧护坡，使山间梯田在俯视地图中形成方向明确的层次。
    /// </summary>
    private static int TerraceLayer(int dx, int dy)
    {
        bool inside = Math.Abs(dx) <= 50 && Math.Abs(dy) <= 45;
        bool step = inside && (Math.Abs(dy + 30) <= 2 || Math.Abs(dy + 10) <= 2 ||
            Math.Abs(dy - 10) <= 2 || Math.Abs(dy - 30) <= 2);
        bool wall = inside && Math.Abs(dx) >= 47;
        return step ? 3 : wall ? 2 : inside ? 1 : 0;
    }

    /// <summary>
    /// 绘制两条交叉道路和四组界石，使荒野路口具有世界方向信息而不是独立图标感。
    /// </summary>
    private static int CrossroadsLayer(int dx, int dy)
    {
        bool road = (Math.Abs(dx) <= 7 || Math.Abs(dy) <= 7) &&
            Math.Abs(dx) <= 58 && Math.Abs(dy) <= 58;
        bool marker = Math.Abs(dx) is >= 17 and <= 21 && Math.Abs(dy) is >= 17 and <= 21;
        return marker ? 3 : road ? 1 : 0;
    }

    /// <summary>
    /// 按目标亮度分位附近选取场景颜色，使结构纹样保留原图色相而不携带横版画面构图。
    /// </summary>
    private static Color Sample(Image source, float targetLuminance)
    {
        Color selected = new(0.2f, 0.2f, 0.2f);
        float bestDistance = float.MaxValue;
        for (int y = 0; y < source.GetHeight(); y += 3)
        {
            for (int x = 0; x < source.GetWidth(); x += 3)
            {
                Color color = source.GetPixel(x, y);
                float luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
                float distance = Math.Abs(luminance - targetLuminance);
                if (color.A >= 0.1f && distance < bestDistance)
                {
                    selected = color;
                    bestDistance = distance;
                }
            }
        }

        return selected;
    }
}
