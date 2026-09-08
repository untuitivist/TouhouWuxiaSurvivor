using Rebirth.Core;
using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private const int JournalPageSize = 6;
    private JournalCategory? journalCategory;
    private int journalPage;
    private string journalSelected = "";
    private readonly Dictionary<string, Texture2D> journalTextures = [];

    private void OpenJournal()
    {
        if (currentScreen is not ("title" or "pause")) return;
        ShowJournal();
    }

    private void ShowJournal()
    {
        var entries = JournalCatalog.All.Where(entry => journalCategory == null || entry.Category == journalCategory).ToArray();
        var pages = Math.Max(1, (entries.Length + JournalPageSize - 1) / JournalPageSize);
        journalPage = Math.Clamp(journalPage, 0, pages - 1);
        var panel = Modal("journal", GameText.Get("FIELD JOURNAL  /  夜境图鉴"), GameText.Get("见其形，知其所长。"), 1160, 660);
        for (var index = 0; index <= Enum.GetValues<JournalCategory>().Length; index++)
        {
            JournalCategory? category = index == 0 ? null : (JournalCategory)(index - 1);
            var label = category.HasValue ? JournalCatalog.CategoryName(category.Value) : GameText.Get("全部");
            var tab = ui.Button(panel, label, new(32 + index * 157, 132, 149, 60), () =>
            {
                journalCategory = category;
                journalPage = 0;
                journalSelected = "";
                ShowJournal();
            }, category == journalCategory);
            tab.Name = "journal_filter_" + (category?.ToString() ?? "All");
        }
        Button? first = null;
        Button? selected = null;
        var visible = entries.Skip(journalPage * JournalPageSize).Take(JournalPageSize).ToArray();
        for (var index = 0; index < visible.Length; index++)
        {
            var entry = visible[index];
            var card = ui.Button(panel, "", new(32 + index % 3 * 368, 208 + index / 3 * 166, 360, 152), () => ShowJournalDetail(entry));
            card.Name = "journal_card_" + entry.Id;
            card.TooltipText = GameText.Get(entry.Name) + GameText.Get(" · 点击查看详情");
            var portrait = new ColorRect { Position = new(12, 12), Size = new(90, 90), Color = Palette.Deep, ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
            card.AddChild(portrait);
            JournalPicture(portrait, entry, new(5, 5, 80, 80));
            ui.Label(card, JournalCatalog.CategoryName(entry.Category), new(116, 12, 220, 24), 16, Palette.Red);
            ui.Label(card, entry.Name, new(116, 42, 228, 60), 23, Palette.Paper, true);
            ui.Label(card, entry.Summary, new(14, 113, 331, 30), 16, Palette.Muted);
            first ??= card;
            if (entry.Id == journalSelected) selected = card;
        }
        var back = ui.Button(panel, GameText.Get("返回"), new(32, 556, 212, 66), NavigateBack);
        var previous = ui.Button(panel, GameText.Get("上一页"), new(474, 556, 176, 66), () => { journalPage--; journalSelected = ""; ShowJournal(); });
        previous.Disabled = journalPage == 0;
        ui.Label(panel, $"{journalPage + 1} / {pages}", new(662, 574, 184, 34), 22, Palette.Gold).HorizontalAlignment = HorizontalAlignment.Center;
        var next = ui.Button(panel, GameText.Get("下一页"), new(860, 556, 268, 66), () => { journalPage++; journalSelected = ""; ShowJournal(); });
        next.Disabled = journalPage == pages - 1;
        ui.Label(panel, GameText.Format($"已实现 {entries.Length} 项 · 点击卡片查看"), new(252, 565, 209, 53), 16, Palette.Muted);
        (selected ?? first ?? back).GrabFocus();
    }

    private void ShowJournalDetail(JournalEntry entry)
    {
        journalSelected = entry.Id;
        var panel = Modal("journal_detail", "FIELD JOURNAL  /  " + JournalCatalog.CategoryName(entry.Category), entry.Name, 1160, 660);
        var portrait = new ColorRect { Position = new(32, 142), Size = new(288, 260), Color = Palette.Deep, ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(portrait);
        JournalPicture(portrait, entry, new(24, 22, 240, 216));
        ui.Label(panel, GameText.Get("局内同源素材 · 静态展示"), new(32, 420, 288, 30), 17, Palette.Gold);
        var detail = new RichTextLabel
        {
            Name = "journal_facts", Position = new(350, 142), Size = new(778, 390),
            BbcodeEnabled = false, SelectionEnabled = true, ScrollActive = true,
            FocusMode = Control.FocusModeEnum.All
        };
        detail.AddThemeFontSizeOverride("normal_font_size", 21);
        detail.AddThemeColorOverride("default_color", PixelSkin.Ink);
        panel.AddChild(detail);
        detail.Text = GameText.Get(entry.Summary) + "\n\n" + GameText.Get(entry.Details) + GameText.Get("\n\n设定与改编\n") + GameText.Get(entry.Source) +
            GameText.Get("\n\n素材路径\n") + JournalCatalog.ArtRoot + entry.Asset +
            (entry.Category == JournalCategory.Training ? GameText.Get("\n\n修习类共用阵纹作分类图标，不表示新增战斗特效。") : "") +
            GameText.Get("\n\n素材仅用于内部原型验证；来源记录不等于公开发行授权。");
        ui.Button(panel, GameText.Get("向上阅读"), new(32, 466, 138, 66), () => detail.GetVScrollBar().Value -= detail.Size.Y * 0.7);
        ui.Button(panel, GameText.Get("向下阅读"), new(182, 466, 138, 66), () => detail.GetVScrollBar().Value += detail.Size.Y * 0.7);
        ui.Button(panel, GameText.Get("返回卡片"), new(32, 556, 288, 66), ShowJournal, true).GrabFocus();
        ui.Label(panel, GameText.Get("滚动阅读全部详情 · 返回保留分类与页码 · Esc 返回"), new(350, 570, 778, 44), 19, Palette.Muted);
    }

    private void JournalPicture(Control parent, JournalEntry entry, Rect2 bounds)
    {
        if (!journalTextures.TryGetValue(entry.Asset, out var texture))
        {
            var source = GD.Load<Texture2D>(JournalCatalog.ArtRoot + entry.Asset);
            texture = entry.Strip ? new AtlasTexture { Atlas = source, Region = new(0, 0, source.GetHeight(), source.GetHeight()) } : source;
            journalTextures.Add(entry.Asset, texture);
        }
        parent.AddChild(new TextureRect
        {
            Name = "journal_picture",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = texture, Position = bounds.Position, Size = bounds.Size,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }
}
