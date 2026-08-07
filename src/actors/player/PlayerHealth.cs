using Godot;

namespace TouhouWuxiaSurvivor.Actors.Player;

/// <summary>
/// 独立管理玩家生命、受伤无敌和闪烁反馈，并向移动、武器与 HUD 暴露只读状态。
/// </summary>
public partial class PlayerHealth : Node
{
    private PlayerVisualController? _visual;
    private double _invincibilityLeft;
    private double _blinkTime;
    private int _baseMaxHealth = 5;

    [Export(PropertyHint.Range, "1,100,1")]
    public int MaxHealth { get; set; } = 5;

    [Export(PropertyHint.Range, "0,10,0.1")]
    public float InvincibilityDuration { get; set; } = 1.0f;

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public bool IsInvincible => _invincibilityLeft > 0.0;
    public event Action<int, int>? HealthChanged;
    public event Action? Damaged;
    public event Action? Died;

    /// <summary>
    /// 获取玩家视觉节点并把当前生命初始化为至少一点的最大生命值。
    /// </summary>
    public override void _Ready()
    {
        _visual = GetNode<PlayerVisualController>("../Visual");
        MaxHealth = Math.Max(1, MaxHealth);
        _baseMaxHealth = MaxHealth;
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// 幂等应用局外最大生命加成，并以相同差值补足当前生命，确保开局 HUD 立即一致。
    /// </summary>
    public void ConfigureMaximumHealthBonus(int bonus)
    {
        int previousMaximum = MaxHealth;
        MaxHealth = Math.Clamp(_baseMaxHealth + Math.Max(0, bonus), 1, 100);
        CurrentHealth = Math.Clamp(CurrentHealth + MaxHealth - previousMaximum, 0, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// 推进无敌时间并切换整个玩家视觉可见性，结束后保证恢复显示。
    /// </summary>
    public override void _Process(double delta)
    {
        if (_invincibilityLeft <= 0.0 || _visual is null)
        {
            return;
        }

        _invincibilityLeft = Math.Max(0.0, _invincibilityLeft - delta);
        _blinkTime += delta;
        _visual.SetBlinkVisible(
            _invincibilityLeft <= 0.0 || ((int)(_blinkTime * 12.0) & 1) == 0);
    }

    /// <summary>
    /// 在存活且不处于无敌状态时扣除生命，并在未死亡时开启一次完整受伤无敌。
    /// </summary>
    public bool ApplyDamage(int amount)
    {
        if (amount <= 0 || IsDead || _invincibilityLeft > 0.0)
        {
            return false;
        }

        CurrentHealth = Math.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (IsDead)
        {
            Died?.Invoke();
        }
        else
        {
            _invincibilityLeft = Math.Max(0.0f, InvincibilityDuration);
            _blinkTime = 0.0;
            Damaged?.Invoke();
        }

        if (_visual is not null && IsDead)
        {
            _visual.SetDefeated();
        }

        return true;
    }

    /// <summary>
    /// 将正数护身时间合并进现有无敌计时，不缩短更长效果，也不复活已死亡角色。
    /// </summary>
    public void GrantInvincibility(float durationSeconds)
    {
        if (durationSeconds <= 0.0f || IsDead)
        {
            return;
        }

        _invincibilityLeft = Math.Max(_invincibilityLeft, durationSeconds);
        _blinkTime = 0.0;
    }
}
