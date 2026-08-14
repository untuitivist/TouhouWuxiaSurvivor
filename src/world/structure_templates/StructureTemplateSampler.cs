namespace TouhouWuxiaSurvivor.World.StructureTemplates;

/// <summary>
/// 在任意占地半径中采样十六类多层俯视模板，结果是结构语义而不是预先着色的方块。
/// </summary>
public static class StructureTemplateSampler
{
    /// <summary>
    /// 把实例坐标变换到 32 单位规范空间后分派模板，保证不同 footprint 保持相似比例。
    /// </summary>
    public static StructureTileRole Sample(
        StructureTemplateKind kind,
        int x,
        int y,
        int radius,
        int quarterTurns,
        int variant)
    {
        (x, y) = StructureTemplateTransform.ToCanonical(x, y, quarterTurns, variant);
        int nx = Scale(x, radius);
        int ny = Scale(y, radius);
        return kind switch
        {
            StructureTemplateKind.Shrine => Shrine(nx, ny),
            StructureTemplateKind.Settlement => Settlement(nx, ny),
            StructureTemplateKind.Circle => Circle(nx, ny),
            StructureTemplateKind.Garden => Garden(nx, ny),
            StructureTemplateKind.Manor => Manor(nx, ny),
            StructureTemplateKind.Terrace => Terrace(nx, ny),
            StructureTemplateKind.Crossroads => Crossroads(nx, ny),
            StructureTemplateKind.Gate => Gate(nx, ny),
            StructureTemplateKind.Ruin => Ruin(nx, ny),
            StructureTemplateKind.Bridge => Bridge(nx, ny),
            StructureTemplateKind.Ship => Ship(nx, ny),
            StructureTemplateKind.Stage => Stage(nx, ny),
            StructureTemplateKind.Tower => Tower(nx, ny),
            StructureTemplateKind.Market => Market(nx, ny),
            StructureTemplateKind.Cave => Cave(nx, ny),
            StructureTemplateKind.Outpost => Outpost(nx, ny),
            _ => StructureTileRole.None,
        };
    }

    /// <summary>将实际占地坐标缩放为 [-32,32] 规范空间，并保留中心细节。</summary>
    private static int Scale(int value, int radius) =>
        (int)Math.Round(value * 32.0 / Math.Max(1, radius));

    /// <summary>生成本殿、庭院、参道与南侧鸟居插槽。</summary>
    private static StructureTileRole Shrine(int x, int y)
    {
        bool hall = Math.Abs(x) <= 19 && y is >= -27 and <= -12;
        bool edge = hall && (Math.Abs(x) >= 18 || y is -27 or -12);
        bool path = Math.Abs(x) <= 4 && y is > -12 and <= 31;
        bool torii = y is >= 19 and <= 20 && Math.Abs(x) <= 14 ||
            Math.Abs(x) is >= 13 and <= 14 && y is >= 19 and <= 27;
        return torii ? StructureTileRole.Socket : edge ? StructureTileRole.Detail :
            hall ? StructureTileRole.Arena : path ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成十字主街、四组院落和中央活动场地。</summary>
    private static StructureTileRole Settlement(int x, int y)
    {
        bool road = Math.Abs(x) <= 3 || Math.Abs(y) <= 3;
        bool house = Math.Abs(x) is >= 10 and <= 27 && Math.Abs(y) is >= 10 and <= 25;
        bool wall = house && (Math.Abs(x) >= 24 || Math.Abs(y) >= 22 ||
            Math.Abs(x) <= 12 || Math.Abs(y) <= 12);
        return wall ? StructureTileRole.Detail : house ? StructureTileRole.Ground :
            road ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成双层法环、四向符线和可承载事件的中心阵眼。</summary>
    private static StructureTileRole Circle(int x, int y)
    {
        int d = x * x + y * y;
        bool outer = d is >= 25 * 25 and <= 30 * 30;
        bool inner = d is >= 12 * 12 and <= 16 * 16;
        bool rays = (Math.Abs(x) <= 1 || Math.Abs(y) <= 1) && d <= 25 * 25;
        return d <= 4 * 4 ? StructureTileRole.Socket : outer || inner ?
            StructureTileRole.Detail : rays ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成不规则外岸、内部庭园、池心和南侧栈道。</summary>
    private static StructureTileRole Garden(int x, int y)
    {
        int d = x * x + y * y + Math.Abs(x * y) / 3;
        bool border = d is >= 24 * 24 and <= 30 * 30;
        bool dock = Math.Abs(x) <= 2 && y is >= 19 and <= 32;
        return border ? StructureTileRole.Detail : dock ? StructureTileRole.Path :
            d <= 5 * 5 ? StructureTileRole.Socket : d < 24 * 24 ?
            StructureTileRole.Ground : StructureTileRole.None;
    }

    /// <summary>生成主馆、左右翼、回廊、前庭和南门。</summary>
    private static StructureTileRole Manor(int x, int y)
    {
        bool center = Math.Abs(x) <= 17 && y is >= -25 and <= 10;
        bool wings = Math.Abs(x) is >= 17 and <= 30 && y is >= -17 and <= 6;
        bool body = center || wings;
        bool wall = body && (Math.Abs(x) >= 27 || y is -25 or -17 or 6 or 10);
        bool path = Math.Abs(x) <= 4 && y is > 10 and <= 32;
        return wall ? StructureTileRole.Detail : body ? StructureTileRole.Arena :
            path ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成四层山阶、侧坡和顶层事件台。</summary>
    private static StructureTileRole Terrace(int x, int y)
    {
        bool inside = Math.Abs(x) <= 29 && Math.Abs(y) <= 28;
        bool step = inside && (Math.Abs(y + 20) <= 1 || Math.Abs(y + 7) <= 1 ||
            Math.Abs(y - 7) <= 1 || Math.Abs(y - 20) <= 1);
        return step ? StructureTileRole.Path : inside && Math.Abs(x) >= 27 ?
            StructureTileRole.Detail : inside && y < -20 ? StructureTileRole.Socket :
            inside ? StructureTileRole.Ground : StructureTileRole.None;
    }

    /// <summary>生成双向道路、四组界石和中央交汇点。</summary>
    private static StructureTileRole Crossroads(int x, int y)
    {
        bool road = Math.Abs(x) <= 4 || Math.Abs(y) <= 4;
        bool marker = Math.Abs(x) is >= 11 and <= 14 && Math.Abs(y) is >= 11 and <= 14;
        return marker ? StructureTileRole.Detail : Math.Abs(x) <= 2 && Math.Abs(y) <= 2 ?
            StructureTileRole.Socket : road ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成双柱横梁、门后短路和门心触发插槽。</summary>
    private static StructureTileRole Gate(int x, int y)
    {
        bool pillars = Math.Abs(x) is >= 18 and <= 23 && y is >= -20 and <= 20;
        bool lintel = Math.Abs(x) <= 27 && y is >= -24 and <= -19;
        bool path = Math.Abs(x) <= 5 && y is >= -18 and <= 32;
        return lintel || pillars ? StructureTileRole.Detail : Math.Abs(x) <= 3 &&
            Math.Abs(y) <= 3 ? StructureTileRole.Socket : path ?
            StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成带断口的围墙、散落残块和空心核心。</summary>
    private static StructureTileRole Ruin(int x, int y)
    {
        bool wall = Math.Abs(x) is >= 20 and <= 26 && Math.Abs(y) <= 26 ||
            Math.Abs(y) is >= 20 and <= 26 && Math.Abs(x) <= 26;
        bool breaks = Math.Abs(x + y) <= 4 || Math.Abs(x - y - 18) <= 4;
        bool debris = Math.Abs(x) is >= 8 and <= 12 && Math.Abs(y) is >= 5 and <= 10;
        return wall && !breaks ? StructureTileRole.Detail : debris ? StructureTileRole.Ground :
            Math.Abs(x) <= 4 && Math.Abs(y) <= 4 ? StructureTileRole.Socket : StructureTileRole.None;
    }

    /// <summary>生成长桥面、两侧护栏、桥头和桥心事件位。</summary>
    private static StructureTileRole Bridge(int x, int y)
    {
        bool deck = Math.Abs(x) <= 7 && Math.Abs(y) <= 32;
        bool rail = Math.Abs(x) is >= 8 and <= 11 && Math.Abs(y) <= 32;
        bool head = Math.Abs(x) <= 18 && Math.Abs(y) is >= 27 and <= 31;
        return rail ? StructureTileRole.Detail : Math.Abs(x) <= 3 && Math.Abs(y) <= 3 ?
            StructureTileRole.Socket : deck || head ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成尖首船体、甲板边沿、中轴桅杆位和船尾入口。</summary>
    private static StructureTileRole Ship(int x, int y)
    {
        int halfWidth = Math.Max(4, 21 - Math.Abs(y) / 2);
        bool hull = Math.Abs(y) <= 31 && Math.Abs(x) <= halfWidth;
        bool rim = hull && Math.Abs(x) >= halfWidth - 3;
        return rim ? StructureTileRole.Detail : Math.Abs(x) <= 2 && y is >= -19 and <= 15 ?
            StructureTileRole.Socket : hull ? StructureTileRole.Arena : StructureTileRole.None;
    }

    /// <summary>生成方形擂台、围边、四角立柱和中心场地。</summary>
    private static StructureTileRole Stage(int x, int y)
    {
        bool floor = Math.Abs(x) <= 27 && Math.Abs(y) <= 24;
        bool border = floor && (Math.Abs(x) >= 24 || Math.Abs(y) >= 21);
        bool post = Math.Abs(x) is >= 23 and <= 27 && Math.Abs(y) is >= 20 and <= 24;
        return post ? StructureTileRole.Socket : border ? StructureTileRole.Detail :
            floor ? StructureTileRole.Arena : StructureTileRole.None;
    }

    /// <summary>生成阶梯式塔基、斜边、南侧入口和塔心插槽。</summary>
    private static StructureTileRole Tower(int x, int y)
    {
        int width = Math.Clamp(11 + (y + 28) / 3, 9, 27);
        bool body = y is >= -29 and <= 27 && Math.Abs(x) <= width;
        bool edge = body && Math.Abs(x) >= width - 3;
        bool door = Math.Abs(x) <= 4 && y is >= 20 and <= 31;
        return edge ? StructureTileRole.Detail : door ? StructureTileRole.Path :
            Math.Abs(x) <= 3 && Math.Abs(y) <= 3 ? StructureTileRole.Socket :
            body ? StructureTileRole.Ground : StructureTileRole.None;
    }

    /// <summary>生成中央市道、两排错落摊位和中心交易位。</summary>
    private static StructureTileRole Market(int x, int y)
    {
        bool lane = Math.Abs(x) <= 5 && Math.Abs(y) <= 32;
        bool stall = Math.Abs(x) is >= 10 and <= 27 && Math.Abs(y % 14) <= 4;
        bool canopy = stall && Math.Abs(y % 14) >= 3;
        return canopy ? StructureTileRole.Detail : stall ? StructureTileRole.Ground :
            Math.Abs(x) <= 3 && Math.Abs(y) <= 3 ? StructureTileRole.Socket :
            lane ? StructureTileRole.Path : StructureTileRole.None;
    }

    /// <summary>生成不规则岩环、矿脉、洞内地面和洞心节点。</summary>
    private static StructureTileRole Cave(int x, int y)
    {
        int d = x * x + y * y + Math.Abs(x * y) / 4;
        bool rock = d is >= 20 * 20 and <= 30 * 30;
        bool vein = Math.Abs(x + y / 2) <= 2 && d <= 25 * 25;
        return rock ? StructureTileRole.Detail : Math.Abs(x) <= 4 && Math.Abs(y) <= 4 ?
            StructureTileRole.Socket : vein ? StructureTileRole.Path : d < 20 * 20 ?
            StructureTileRole.Ground : StructureTileRole.None;
    }

    /// <summary>生成带南门的围墙、中央哨所、庭院和中心事件位。</summary>
    private static StructureTileRole Outpost(int x, int y)
    {
        bool wall = Math.Abs(x) is >= 25 and <= 29 && Math.Abs(y) <= 29 ||
            Math.Abs(y) is >= 25 and <= 29 && Math.Abs(x) <= 29;
        bool gate = Math.Abs(x) <= 5 && y is >= 24 and <= 31;
        bool keep = Math.Abs(x) <= 13 && y is >= -17 and <= 5;
        return wall && !gate ? StructureTileRole.Detail : keep ? StructureTileRole.Arena :
            Math.Abs(x) <= 3 && Math.Abs(y + 6) <= 3 ? StructureTileRole.Socket :
            gate ? StructureTileRole.Path : StructureTileRole.None;
    }
}
