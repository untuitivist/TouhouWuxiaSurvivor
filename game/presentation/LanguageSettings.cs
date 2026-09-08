using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void AddLanguageSelector(Control panel)
    {
        ui.Label(panel, GameText.Get("语言"), new(544, 40, 122, 40), 18, Palette.Muted);
        var languages = new OptionButton
        {
            Name = "game_language", Position = new(680, 29), Size = new(268, 60),
            TooltipText = "语言 / Language"
        };
        languages.AddItem("简体中文");
        languages.AddItem("English");
        languages.Select(GameText.IsEnglish ? 1 : 0);
        panel.AddChild(languages);
        languages.ItemSelected += selected =>
        {
            var draft = videoDraft.Copy();
            profile.Data.Language = selected == 1 ? "en" : "zh";
            settingsMessage = "";
            GameText.SetLanguage(profile.Data.Language);
            GameControls.Configure(profile.Data.Bindings);
            DisplayServer.WindowSetTitle(GameText.Get("幻想乡 · 夜境异闻"));
            SaveSettings();
            if (settingsTab == 1 && !GamePlatform.IsWeb)
            {
                videoDraft = draft;
                RefreshVideoOptions();
            }
            canvas.RefreshLanguage();
            touchHud.QueueRedraw();
            Descendants(screen!).OfType<OptionButton>().Single(option => option.Name == "game_language").GrabFocus();
        };
    }
}
