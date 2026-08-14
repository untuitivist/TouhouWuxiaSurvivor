using Godot;
using TouhouWuxiaSurvivor.Versioning;
using TouhouWuxiaSurvivor.Versioning.Changelog;

namespace TouhouWuxiaSurvivor.Ui.Changelog;

/// <summary>
/// 在主菜单内提供固定两栏版本日志，左侧选择版本，右侧只滚动该版本正文。
/// </summary>
public partial class ChangelogPanel : Control
{
    private GameChangelogEntry[] _entries = [];
    private ItemList? _versions;
    private Label? _entryTitle;
    private Label? _entryMeta;
    private RichTextLabel? _body;

    public event Action? BackRequested;
    public int EntryCount => _entries.Length;
    public string SelectedVersion { get; private set; } = string.Empty;
    public string CurrentBodyText { get; private set; } = string.Empty;

    /// <summary>加载唯一日志资源、连接列表与返回事件，并保持面板初始隐藏。</summary>
    public override void _Ready()
    {
        _versions = GetNode<ItemList>("Panel/Padding/Layout/Browser/Index/Versions");
        _entryTitle = GetNode<Label>("Panel/Padding/Layout/Browser/Detail/Identity/EntryTitle");
        _entryMeta = GetNode<Label>("Panel/Padding/Layout/Browser/Detail/Identity/EntryMeta");
        _body = GetNode<RichTextLabel>("Panel/Padding/Layout/Browser/Detail/Body");
        GetNode<Label>("Panel/Padding/Layout/Header/Current").Text = $"当前  {GameVersion.Current}";
        GetNode<Button>("Panel/Padding/Layout/Header/Back").Pressed += RequestBack;
        _versions.ItemSelected += OnVersionSelected;
        LoadEntries();
        SetProcessUnhandledInput(true);
        Hide();
    }

    /// <summary>显示日志并始终回到最新版本，避免上次历史选择让玩家误认当前版本。</summary>
    public void Present()
    {
        Show();
        if (_entries.Length > 0)
        {
            _versions!.Select(0);
            ShowEntry(0);
            _versions.GrabFocus();
        }
    }

    /// <summary>日志可见时允许 Esc 返回主菜单，并消费事件避免同时触发其他界面。</summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible || !inputEvent.IsActionPressed("ui_cancel"))
        {
            return;
        }

        RequestBack();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>读取完整目录并构建从新到旧的版本索引；失败时保留可返回的错误页。</summary>
    private void LoadEntries()
    {
        try
        {
            _entries = GameChangelogCatalog.LoadDefault().Entries.ToArray();
            foreach (GameChangelogEntry entry in _entries)
            {
                _versions!.AddItem(entry.Version == GameVersion.Current
                    ? $"{entry.Version}  当前"
                    : entry.Version);
            }
        }
        catch (Exception exception)
        {
            GD.PushWarning($"版本日志加载失败: {exception.Message}");
            _entryTitle!.Text = "版本日志不可用";
            _entryMeta!.Text = string.Empty;
            _body!.Text = "日志资源缺失或格式错误，请保留完整游戏文件后重试。";
        }
    }

    /// <summary>把列表信号的长整型索引安全转换为版本数组索引。</summary>
    private void OnVersionSelected(long index) => ShowEntry((int)index);

    /// <summary>刷新版本标题、统计和正文，并将正文滚动位置重置到开头。</summary>
    private void ShowEntry(int index)
    {
        if (index < 0 || index >= _entries.Length)
        {
            return;
        }

        GameChangelogEntry entry = _entries[index];
        SelectedVersion = entry.Version;
        CurrentBodyText = GameChangelogTextFormatter.ToPlainText(entry);
        _entryTitle!.Text = entry.Version;
        _entryMeta!.Text = $"{entry.Sections.Count} 节  ·  {entry.ItemCount} 项";
        _body!.Text = GameChangelogTextFormatter.ToBbCode(entry);
        _body.ScrollToLine(0);
    }

    /// <summary>隐藏日志并通知主菜单恢复命令区，保证同一时刻仅一个主菜单页面可见。</summary>
    private void RequestBack()
    {
        Hide();
        BackRequested?.Invoke();
    }
}
