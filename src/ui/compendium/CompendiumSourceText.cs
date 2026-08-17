using TouhouWuxiaSurvivor.Content;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>统一图鉴来源 ID 与中文作品标签，避免各类投影器重复判断本体编号。</summary>
public static class CompendiumSourceText
{
    /// <summary>返回本体或可选内容包在图鉴筛选器中使用的稳定来源 ID。</summary>
    public static string GetId(ContentPackDefinition source) =>
        source.Number == 0 ? CompendiumCatalog.BaseSourceId : source.Id;

    /// <summary>返回本体名称或带 TH 编号的可选内容包名称。</summary>
    public static string GetLabel(ContentPackDefinition source) =>
        source.Number == 0 ? source.DisplayName : $"TH{source.Number:00} {source.DisplayName}";
}
