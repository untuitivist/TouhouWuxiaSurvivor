namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 表示图鉴详情中的一个稳定属性键值，并显式声明不定长内容是否独占整行。
/// </summary>
public sealed record CompendiumFact(string Label, string Value, bool IsWide = false);
