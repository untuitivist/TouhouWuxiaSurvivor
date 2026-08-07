using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 手绘十二类敌人和四类日常人物的双帧像素精灵表。
/// </summary>
internal sealed class PreviewSpritePainter
{
    private const int Cell = 16;
    private static readonly Rgba32 Transparent = new(0, 0, 0, 0);
    private static readonly Rgba32 Outline = new(8, 10, 8, 255);

    /// <summary>
    /// 每种敌人占连续两个 16x16 帧，通过轮廓参数表现飞行、虫、兽与灵体差异。
    /// </summary>
    public PixelCanvas PaintEnemySheet()
    {
        var canvas = new PixelCanvas(Cell * 24, Cell);
        canvas.Fill(Transparent);
        Rgba32[] colors =
        [
            new(174, 221, 241), new(231, 229, 214), new(156, 190, 91),
            new(201, 145, 86), new(86, 170, 103), new(148, 181, 77),
            new(133, 151, 154), new(177, 112, 78), new(145, 116, 181),
            new(204, 104, 186), new(142, 126, 196), new(217, 84, 76),
        ];
        for (int archetype = 0; archetype < colors.Length; archetype++)
        {
            DrawEnemy(canvas, archetype * Cell * 2, colors[archetype], archetype, 0);
            DrawEnemy(canvas, archetype * Cell * 2 + Cell, colors[archetype], archetype, 1);
        }

        return canvas;
    }

    /// <summary>
    /// 生成巫女、村民、妖精和幽灵四类日常人物，每类提供左右脚交替帧。
    /// </summary>
    public PixelCanvas PaintDailyActorSheet()
    {
        var canvas = new PixelCanvas(Cell * 8, Cell);
        canvas.Fill(Transparent);
        Rgba32[] clothes =
        [
            new(183, 47, 39), new(77, 112, 73), new(91, 145, 177), new(137, 104, 177),
        ];
        for (int actor = 0; actor < clothes.Length; actor++)
        {
            DrawActor(canvas, actor * Cell * 2, clothes[actor], actor, 0);
            DrawActor(canvas, actor * Cell * 2 + Cell, clothes[actor], actor, 1);
        }

        return canvas;
    }

    /// <summary>
    /// 根据原型编号调整耳、翼、角、尾与灵火，并用第二帧改变步幅和悬浮高度。
    /// </summary>
    private static void DrawEnemy(
        PixelCanvas canvas, int originX, Rgba32 color, int archetype, int frame)
    {
        int bob = frame == 0 ? 0 : -1;
        PixelDrawing.FillEllipse(canvas, originX + 8, 8 + bob, 4, 4, Outline);
        PixelDrawing.FillEllipse(canvas, originX + 8, 8 + bob, 3, 3, color);
        if (archetype is 0 or 4 or 9 or 10)
        {
            PixelDrawing.FillTriangle(canvas, (originX + 4, 7 + bob), (originX + 1, 4 + frame),
                (originX + 3, 11 + bob), color);
            PixelDrawing.FillTriangle(canvas, (originX + 12, 7 + bob), (originX + 15, 4 + frame),
                (originX + 13, 11 + bob), color);
        }
        else if (archetype is 2 or 5 or 11)
        {
            PixelDrawing.Line(canvas, originX + 5, 5 + bob, originX + 2, 2 + frame, color);
            PixelDrawing.Line(canvas, originX + 11, 5 + bob, originX + 14, 2 + frame, color);
            PixelDrawing.FillRect(canvas, originX + 6, 11 + bob, 4, 3, color);
        }
        else if (archetype is 3 or 6 or 7)
        {
            PixelDrawing.FillTriangle(canvas, (originX + 5, 5 + bob), (originX + 4, 1 + frame),
                (originX + 7, 4 + bob), color);
            PixelDrawing.FillTriangle(canvas, (originX + 11, 5 + bob), (originX + 12, 1 + frame),
                (originX + 9, 4 + bob), color);
            canvas.SetPixel(originX + 8 + (frame == 0 ? -5 : 5), 11, color);
        }
        else
        {
            PixelDrawing.Line(canvas, originX + 5, 12 + bob, originX + 3, 15, color);
            PixelDrawing.Line(canvas, originX + 11, 12 + bob, originX + 13, 15, color);
        }

        if (archetype is 3 or 6 or 7)
        {
            PixelDrawing.FillRect(canvas, originX + 4 + frame, 12, 3, 2, Outline);
            PixelDrawing.FillRect(canvas, originX + 10 - frame, 12, 3, 2, Outline);
        }
        else if (archetype == 8)
        {
            PixelDrawing.Line(canvas, originX + 8, 12 + bob, originX + 5 + frame * 5, 15, color);
        }
        else if (archetype == 11)
        {
            PixelDrawing.FillRect(canvas, originX + 7, 6 + bob, 3, 4,
                new Rgba32(239, 157, 55));
        }

        canvas.SetPixel(originX + 6, 8 + bob, new Rgba32(245, 225, 165));
        canvas.SetPixel(originX + 10, 8 + bob, new Rgba32(245, 225, 165));
    }

    /// <summary>
    /// 绘制可在地区日常预览中行走的简化人物，并为妖精与幽灵附加识别轮廓。
    /// </summary>
    private static void DrawActor(
        PixelCanvas canvas, int originX, Rgba32 clothes, int actor, int frame)
    {
        Rgba32 skin = new(225, 194, 157);
        PixelDrawing.FillRect(canvas, originX + 6, 3, 5, 4, Outline);
        PixelDrawing.FillRect(canvas, originX + 7, 3, 3, 3, skin);
        PixelDrawing.FillTriangle(canvas, (originX + 5, 7), (originX + 12, 7),
            (originX + 8, 13), clothes);
        PixelDrawing.Line(canvas, originX + 7, 12, originX + 5 + frame * 2, 15, Outline);
        PixelDrawing.Line(canvas, originX + 10, 12, originX + 12 - frame * 2, 15, Outline);
        if (actor == 0)
        {
            PixelDrawing.FillRect(canvas, originX + 5, 1, 7, 2, new Rgba32(210, 54, 47));
            PixelDrawing.FillRect(canvas, originX + 3, 8 + frame, 3, 4, clothes);
            PixelDrawing.FillRect(canvas, originX + 11, 9 - frame, 3, 4, clothes);
        }
        else if (actor == 1)
        {
            PixelDrawing.Line(canvas, originX + 4, 3, originX + 12, 3,
                new Rgba32(173, 139, 82));
            PixelDrawing.FillTriangle(canvas, (originX + 5, 3), (originX + 8, 0),
                (originX + 12, 3), new Rgba32(173, 139, 82));
            PixelDrawing.FillRect(canvas, originX + 12, 8, 3, 4, new Rgba32(117, 78, 45));
        }
        else if (actor == 2)
        {
            PixelDrawing.FillTriangle(canvas, (originX + 5, 7), (originX + 1, 3 + frame),
                (originX + 2, 9), new Rgba32(128, 202, 217, 200));
            PixelDrawing.FillTriangle(canvas, (originX + 12, 7), (originX + 15, 3 + frame),
                (originX + 14, 9), new Rgba32(128, 202, 217, 200));
        }
        else if (actor == 3)
        {
            PixelDrawing.FillEllipse(canvas, originX + 8, 12, 5, 2, new Rgba32(137, 104, 177, 120));
            PixelDrawing.Line(canvas, originX + 8, 13, originX + 5 + frame * 6, 15,
                new Rgba32(185, 157, 222, 180));
        }
    }
}
