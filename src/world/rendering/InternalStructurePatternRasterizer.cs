namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 把结构语义栅格化为三层俯视像素轮廓；返回值 0 透明、1 主体、2 阴影、3 高光。
/// </summary>
internal static class InternalStructurePatternRasterizer
{
    /// <summary>
    /// 将已解析形态分派到独立轮廓算法，确保每种结构拥有稳定占地且不依赖纹理尺寸。
    /// </summary>
    internal static int GetLayer(InternalStructureShape shape, int dx, int dy) => shape switch
    {
        InternalStructureShape.Shrine => Shrine(dx, dy),
        InternalStructureShape.Settlement => Settlement(dx, dy),
        InternalStructureShape.Circle => Circle(dx, dy),
        InternalStructureShape.Garden => Garden(dx, dy),
        InternalStructureShape.Manor => Manor(dx, dy),
        InternalStructureShape.Terrace => Terrace(dx, dy),
        InternalStructureShape.Crossroads => Crossroads(dx, dy),
        InternalStructureShape.Gate => Gate(dx, dy),
        InternalStructureShape.Ruin => Ruin(dx, dy),
        InternalStructureShape.Bridge => Bridge(dx, dy),
        InternalStructureShape.Ship => Ship(dx, dy),
        InternalStructureShape.Stage => Stage(dx, dy),
        InternalStructureShape.Tower => Tower(dx, dy),
        InternalStructureShape.Market => Market(dx, dy),
        InternalStructureShape.Cave => Cave(dx, dy),
        InternalStructureShape.Outpost => Outpost(dx, dy),
        _ => 0,
    };

    /// <summary>绘制北侧本殿、参道和南侧鸟居，适合博丽与守矢系神社。</summary>
    private static int Shrine(int dx, int dy)
    {
        bool hall = Math.Abs(dx) <= 38 && dy is >= -46 and <= -18;
        bool border = hall && (Math.Abs(dx) >= 34 || dy is -46 or -18);
        bool path = Math.Abs(dx) <= 7 && dy is > -18 and <= 60;
        bool torii = dy is >= 27 and <= 31 && Math.Abs(dx) <= 26 ||
            Math.Abs(dx) is >= 21 and <= 25 && dy is >= 27 and <= 48;
        return torii ? 3 : border ? 2 : hall || path ? 1 : 0;
    }

    /// <summary>绘制十字主街和四角房舍，表现人里、旧都与魔界城市。</summary>
    private static int Settlement(int dx, int dy)
    {
        bool road = (Math.Abs(dx) <= 6 || Math.Abs(dy) <= 6) && Math.Abs(dx) <= 56 && Math.Abs(dy) <= 56;
        bool house = Math.Abs(dx) is >= 18 and <= 46 && Math.Abs(dy) is >= 18 and <= 44;
        bool wall = house && (Math.Abs(dx) is >= 40 or <= 22 || Math.Abs(dy) is >= 38 or <= 22);
        return wall ? 2 : house ? 3 : road ? 1 : 0;
    }

    /// <summary>绘制双层圆环、四向符线和核心，表现祭坛、法阵与异质核心。</summary>
    private static int Circle(int dx, int dy)
    {
        int distance = dx * dx + dy * dy;
        bool outer = distance is >= 42 * 42 and <= 47 * 47;
        bool inner = distance is >= 21 * 21 and <= 25 * 25;
        bool rays = (Math.Abs(dx) <= 2 || Math.Abs(dy) <= 2) && distance <= 42 * 42;
        return outer ? 2 : inner || rays ? 3 : distance <= 8 * 8 ? 1 : 0;
    }

    /// <summary>绘制自然外岸、中央庭园与短栈道，表现花田、庭园和湖岛。</summary>
    private static int Garden(int dx, int dy)
    {
        int distance = dx * dx + dy * dy + Math.Abs(dx * dy) / 3;
        bool border = distance is >= 37 * 37 and <= 46 * 46;
        bool dock = Math.Abs(dx) <= 3 && dy is >= 28 and <= 58;
        return border ? 2 : dock ? 3 : distance < 37 * 37 ? 1 : 0;
    }

    /// <summary>绘制主馆、左右翼和入口，表现宅邸、宫殿与大型寺院。</summary>
    private static int Manor(int dx, int dy)
    {
        bool center = Math.Abs(dx) <= 28 && dy is >= -38 and <= 20;
        bool wings = Math.Abs(dx) is >= 28 and <= 52 && dy is >= -25 and <= 12;
        bool entrance = Math.Abs(dx) <= 7 && dy is > 20 and <= 57;
        bool wall = (center || wings) && (Math.Abs(dx) >= 47 || dy is -38 or -25 or 12 or 20);
        return wall ? 2 : center || wings ? 3 : entrance ? 1 : 0;
    }

    /// <summary>绘制多层水平山阶和两侧护坡，表现山间梯田与分层平台。</summary>
    private static int Terrace(int dx, int dy)
    {
        bool inside = Math.Abs(dx) <= 50 && Math.Abs(dy) <= 45;
        bool step = inside && (Math.Abs(dy + 30) <= 2 || Math.Abs(dy + 10) <= 2 ||
            Math.Abs(dy - 10) <= 2 || Math.Abs(dy - 30) <= 2);
        return step ? 3 : inside && Math.Abs(dx) >= 47 ? 2 : inside ? 1 : 0;
    }

    /// <summary>绘制交叉道路和四组界石，表现古道、梦境边界与兽道标记。</summary>
    private static int Crossroads(int dx, int dy)
    {
        bool road = (Math.Abs(dx) <= 7 || Math.Abs(dy) <= 7) && Math.Abs(dx) <= 58 && Math.Abs(dy) <= 58;
        bool marker = Math.Abs(dx) is >= 17 and <= 21 && Math.Abs(dy) is >= 17 and <= 21;
        return marker ? 3 : road ? 1 : 0;
    }

    /// <summary>绘制双柱、横梁和门后短路，表现结界门、地狱门与月都转移门。</summary>
    private static int Gate(int dx, int dy)
    {
        bool pillars = Math.Abs(dx) is >= 30 and <= 38 && dy is >= -34 and <= 35;
        bool lintel = Math.Abs(dx) <= 44 && dy is >= -38 and <= -29;
        bool path = Math.Abs(dx) <= 8 && dy is >= -28 and <= 58;
        return lintel ? 3 : pillars ? 2 : path ? 1 : 0;
    }

    /// <summary>绘制断裂围墙、散落残块和空心核心，表现废墟与弃置器物。</summary>
    private static int Ruin(int dx, int dy)
    {
        bool wall = (Math.Abs(dx) is >= 34 and <= 43 && Math.Abs(dy) <= 42) ||
            (Math.Abs(dy) is >= 34 and <= 43 && Math.Abs(dx) <= 42);
        bool breaks = dx + dy is >= -8 and <= 8 || dx - dy is >= 24 and <= 36;
        bool debris = Math.Abs(dx) is >= 12 and <= 18 && Math.Abs(dy) is >= 8 and <= 15;
        return wall && !breaks ? 2 : debris ? 3 : Math.Abs(dx) <= 9 && Math.Abs(dy) <= 9 ? 1 : 0;
    }

    /// <summary>绘制长桥面、两侧护栏和桥头，表现嫉妒桥等线性通道。</summary>
    private static int Bridge(int dx, int dy)
    {
        bool deck = Math.Abs(dx) <= 12 && Math.Abs(dy) <= 58;
        bool rail = Math.Abs(dx) is >= 13 and <= 17 && Math.Abs(dy) <= 58;
        bool head = Math.Abs(dx) <= 28 && Math.Abs(dy) is >= 48 and <= 56;
        return rail ? 2 : head ? 3 : deck ? 1 : 0;
    }

    /// <summary>绘制尖首船体、中轴甲板和尾翼，表现圣辇船与概率空间船。</summary>
    private static int Ship(int dx, int dy)
    {
        int halfWidth = Math.Max(5, 34 - Math.Abs(dy) / 2);
        bool hull = Math.Abs(dy) <= 55 && Math.Abs(dx) <= halfWidth;
        bool rim = hull && Math.Abs(dx) >= halfWidth - 3;
        bool mast = Math.Abs(dx) <= 3 && dy is >= -36 and <= 26;
        return rim ? 2 : mast ? 3 : hull ? 1 : 0;
    }

    /// <summary>绘制方形擂台、四角立柱和中心标记，表现竞技场与演奏舞台。</summary>
    private static int Stage(int dx, int dy)
    {
        bool floor = Math.Abs(dx) <= 46 && Math.Abs(dy) <= 40;
        bool border = floor && (Math.Abs(dx) >= 42 || Math.Abs(dy) >= 36);
        bool post = Math.Abs(dx) is >= 39 and <= 45 && Math.Abs(dy) is >= 33 and <= 39;
        return post ? 3 : border ? 2 : floor ? 1 : 0;
    }

    /// <summary>绘制阶梯式高塔、尖顶与入口，表现辉针城和地下金字塔。</summary>
    private static int Tower(int dx, int dy)
    {
        int width = Math.Max(8, 48 - (44 - dy) / 3);
        bool body = dy is >= -48 and <= 44 && Math.Abs(dx) <= width;
        bool edge = body && Math.Abs(dx) >= width - 4;
        bool door = Math.Abs(dx) <= 7 && dy is >= 25 and <= 48;
        return edge ? 2 : door ? 3 : body ? 1 : 0;
    }

    /// <summary>绘制中央通路和两排摊位，表现卡牌市场、虹龙洞市场与地狱摊位。</summary>
    private static int Market(int dx, int dy)
    {
        bool lane = Math.Abs(dx) <= 8 && Math.Abs(dy) <= 58;
        bool stall = Math.Abs(dx) is >= 17 and <= 47 && Math.Abs(dy % 24) <= 7;
        bool canopy = stall && Math.Abs(dy % 24) is >= 5 and <= 7;
        return canopy ? 3 : stall ? 2 : lane ? 1 : 0;
    }

    /// <summary>绘制不规则岩环、内部洞口和矿脉，表现工房、矿洞与法界封印。</summary>
    private static int Cave(int dx, int dy)
    {
        int distance = dx * dx + dy * dy + Math.Abs(dx * dy) / 4;
        bool rock = distance is >= 31 * 31 and <= 49 * 49;
        bool vein = Math.Abs(dx + dy / 2) <= 3 && distance <= 42 * 42;
        return rock ? 2 : vein ? 3 : distance < 31 * 31 ? 1 : 0;
    }

    /// <summary>绘制围墙、单侧入口和中央哨所，表现关卡、亭台与前线据点。</summary>
    private static int Outpost(int dx, int dy)
    {
        bool wall = Math.Abs(dx) is >= 42 and <= 48 && Math.Abs(dy) <= 48 ||
            Math.Abs(dy) is >= 42 and <= 48 && Math.Abs(dx) <= 48;
        bool gate = Math.Abs(dx) <= 8 && dy is >= 38 and <= 50;
        bool keep = Math.Abs(dx) <= 20 && dy is >= -25 and <= 8;
        return wall && !gate ? 2 : keep ? 3 : gate ? 1 : 0;
    }
}
