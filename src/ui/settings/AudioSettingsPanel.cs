using Godot;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Ui.Settings;

/// <summary>
/// 管理主音量、音乐和音效三个滑块，并将线性百分比实时应用到对应音频总线。
/// </summary>
public partial class AudioSettingsPanel : VBoxContainer
{
    private HSlider? _masterSlider;
    private HSlider? _musicSlider;
    private HSlider? _sfxSlider;
    private Label? _masterValue;
    private Label? _musicValue;
    private Label? _sfxValue;

    /// <summary>
    /// 读取控件与当前设置，在连接信号前初始化数值，避免打开面板时触发无意义保存。
    /// </summary>
    public override void _Ready()
    {
        GameSettingsService.Initialize();
        _masterSlider = GetNode<HSlider>("Master/Slider");
        _musicSlider = GetNode<HSlider>("Music/Slider");
        _sfxSlider = GetNode<HSlider>("Sfx/Slider");
        _masterValue = GetNode<Label>("Master/Value");
        _musicValue = GetNode<Label>("Music/Value");
        _sfxValue = GetNode<Label>("Sfx/Value");

        _masterSlider.Value = GameSettingsService.Current.MasterVolume * 100.0;
        _musicSlider.Value = GameSettingsService.Current.MusicVolume * 100.0;
        _sfxSlider.Value = GameSettingsService.Current.SfxVolume * 100.0;
        UpdateLabels();

        _masterSlider.ValueChanged += OnMasterChanged;
        _musicSlider.ValueChanged += OnMusicChanged;
        _sfxSlider.ValueChanged += OnSfxChanged;
    }

    /// <summary>
    /// 更新主音量、应用音频总线并持久化设置。
    /// </summary>
    private void OnMasterChanged(double value)
    {
        GameSettingsService.Current.MasterVolume = (float)value / 100.0f;
        ApplyAndSave();
    }

    /// <summary>
    /// 更新音乐总线音量、应用并持久化设置。
    /// </summary>
    private void OnMusicChanged(double value)
    {
        GameSettingsService.Current.MusicVolume = (float)value / 100.0f;
        ApplyAndSave();
    }

    /// <summary>
    /// 更新音效总线音量、应用并持久化设置。
    /// </summary>
    private void OnSfxChanged(double value)
    {
        GameSettingsService.Current.SfxVolume = (float)value / 100.0f;
        ApplyAndSave();
    }

    /// <summary>
    /// 统一刷新三个音量文本、应用总线并写入设置文件。
    /// </summary>
    private void ApplyAndSave()
    {
        UpdateLabels();
        GameSettingsService.ApplyAudio();
        GameSettingsService.Save();
    }

    /// <summary>
    /// 将滑块浮点值格式化为整数百分比，保持界面读数稳定。
    /// </summary>
    private void UpdateLabels()
    {
        if (_masterSlider is null || _musicSlider is null || _sfxSlider is null ||
            _masterValue is null || _musicValue is null || _sfxValue is null)
        {
            return;
        }

        _masterValue.Text = $"{Mathf.RoundToInt(_masterSlider.Value)}%";
        _musicValue.Text = $"{Mathf.RoundToInt(_musicSlider.Value)}%";
        _sfxValue.Text = $"{Mathf.RoundToInt(_sfxSlider.Value)}%";
    }
}
