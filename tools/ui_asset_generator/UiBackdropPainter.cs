using TouhouWuxiaSurvivor.Tools.TileGenerator;
using BackdropDrawing = TouhouWuxiaSurvivor.Tools.UiAssetGenerator.UiBackdropPixelDrawing;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 组合主菜单的幻想乡夜景，让地景、神社建筑和空气细节分别由独立绘制器维护。
/// </summary>
internal sealed class UiBackdropPainter
{
    /// <summary>
    /// 在 640×360 原生画布上依照远景到前景的顺序生成完整不透明像素场景。
    /// </summary>
    public PixelCanvas PaintInkMountains()
    {
        var canvas = new PixelCanvas(640, 360);
        new UiBackdropLandscapePainter().Draw(canvas);
        new UiBackdropShrinePainter().Draw(canvas);
        new UiBackdropAtmospherePainter().Draw(canvas);
        return canvas;
    }

}
