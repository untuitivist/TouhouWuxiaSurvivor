using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Rebirth.Core;

var failures = new List<string>();
var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) failures.Add(message);
}
void English(string text, string context)
{
    var translated = GameText.Get(text);
    Check(!translated.Any(character => character >= 0x3400 && character <= 0x9fff), context + ": " + translated);
}
GameText.SetLanguage(null);
Check(GameText.Language == "zh", "Missing locale defaults to Chinese");
Check(GameText.Get("风止，夜未尽。") == "歇一会儿？", "Chinese copy revision");
Check(GameText.Get("未知文本") == "未知文本", "Unknown Chinese is retained");
Check(GameText.Format($"执此道 · {"博丽灵梦"}") == "选择 博丽灵梦", "Chinese interpolated copy");
GameText.SetLanguage("EN_us");
Check(GameText.Language == "en", "Regional English normalizes");
Check(GameText.Get("博丽灵梦") == "Reimu Hakurei", "Character name");
Check(GameText.Format($"执此道 · {"博丽灵梦"}") == "Play as Reimu Hakurei", "Nested name in template");
Check(GameText.Get("解锁 · 阴阳玉") == "Unlock · Yin-Yang Orbs", "Composed catalog name");
Check(GameText.Get("一次领悟 · 已获得 御札 · 修习 3") == "Learn once · Requires Ofuda · Level 3", "Nested requirement template");
var beforeCulture = CultureInfo.CurrentCulture;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
Check(GameText.Format($"{{{2.5:0.0}}}") == "{2.5}", "Escaped braces and invariant numbers");
CultureInfo.CurrentCulture = beforeCulture;
Check(GameText.Get("res://assets/players/reimu.png") == "res://assets/players/reimu.png", "Asset paths unchanged");
var catalog = (Dictionary<string, string>)typeof(GameText).GetField("English", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
var token = new Regex(@"\{\d+(?:,[^}:]+)?(?::[^}]+)?\}");
foreach (var entry in catalog)
{
    English(entry.Key, "Catalog");
    if (!token.IsMatch(entry.Key)) continue;
    var source = token.Replace(entry.Key, "2");
    var expected = token.Replace(entry.Value, "2");
    Check(GameText.Get(source) == expected, "Template round trip: " + entry.Key + " => " + GameText.Get(source));
}
foreach (var art in ArtCatalog.All)
{
    foreach (var text in new[] { art.Name, art.School, art.Description, art.Mastery, art.Source, art.Symbol }) English(text, art.Id.ToString());
    for (var rank = 0; rank <= Math.Min(art.MaxRank, 5); rank++)
    {
        English(ArtCatalog.UpgradeText(art.Id, rank), "Rank effect " + art.Id);
        English(ArtCatalog.RankText(art.Id, rank), "Rank label " + art.Id);
    }
}
foreach (var upgrade in UpgradeCatalog.All)
{
    foreach (var text in new[] { upgrade.Name, upgrade.Description, upgrade.Category, UpgradeCatalog.Requirement(upgrade) }) English(text, upgrade.Id);
    English(UpgradeCatalog.Progress(upgrade, new RunState(HeroKind.Reimu, 42).Build), "Progress " + upgrade.Id);
}
for (var index = 0; index < 10; index++)
{
    GameText.SetLanguage("zh-CN");
    Check(GameText.Get("博丽灵梦") == "博丽灵梦", "Switch back to Chinese");
    GameText.SetLanguage("en");
    Check(GameText.Get("解锁 · 阴阳玉") == "Unlock · Yin-Yang Orbs", "Switch back to English");
}
GameText.SetLanguage("invalid");
Check(GameText.Language == "zh", "Invalid locale fallback");
foreach (var failure in failures) Console.Error.WriteLine("FAIL " + failure);
Console.WriteLine($"LOCALIZATION: {checks - failures.Count}/{checks} checks passed");
return failures.Count == 0 ? 0 : 1;
