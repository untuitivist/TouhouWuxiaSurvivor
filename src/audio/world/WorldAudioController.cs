using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

namespace TouhouWuxiaSurvivor.Audio.World;

/// <summary>
/// 统一把世界玩法事件映射到音乐与音效播放器，隔离音频资源、播放策略和战斗实现。
/// </summary>
public partial class WorldAudioController : Node
{
    private const double MinimumShotSoundInterval = 0.05;
    private AudioStreamPlayer? _bgm;
    private AudioStreamPlayer? _shot;
    private AudioStreamPlayer? _footstep;
    private AudioStreamPlayer? _pickup;
    private AudioStreamPlayer? _enemyHit;
    private AudioStreamPlayer? _enemyDeath;
    private AudioStreamPlayer? _explosion;
    private AudioStreamPlayer? _playerHurt;
    private AudioStreamPlayer? _playerDeath;
    private PlayerController? _player;
    private PlayerHealth? _health;
    private AutoShooter? _shooter;
    private EnemySpawner? _enemySpawner;
    private PickupSpawner? _pickupSpawner;
    private double _shotSoundCooldown;

    public int ShotSoundCount { get; private set; }
    public int PickupSoundCount { get; private set; }
    public int EnemyHitSoundCount { get; private set; }
    public int EnemyDeathSoundCount { get; private set; }
    public int PlayerHurtSoundCount { get; private set; }
    public int PlayerDeathSoundCount { get; private set; }
    public bool IsBgmPlaying => _bgm?.Playing == true;

    /// <summary>
    /// 缓存按用途拆分的音频播放器，并在进入游戏页时仅播放一次示例游戏的开场音乐。
    /// </summary>
    public override void _Ready()
    {
        _bgm = GetNode<AudioStreamPlayer>("Bgm");
        _shot = GetNode<AudioStreamPlayer>("Shot");
        _footstep = GetNode<AudioStreamPlayer>("Footstep");
        _pickup = GetNode<AudioStreamPlayer>("Pickup");
        _enemyHit = GetNode<AudioStreamPlayer>("EnemyHit");
        _enemyDeath = GetNode<AudioStreamPlayer>("EnemyDeath");
        _explosion = GetNode<AudioStreamPlayer>("Explosion");
        _playerHurt = GetNode<AudioStreamPlayer>("PlayerHurt");
        _playerDeath = GetNode<AudioStreamPlayer>("PlayerDeath");
        _bgm.Play();
    }

    /// <summary>
    /// 注入本局玩法服务并订阅稳定的汇总事件，避免音频层逐个持有动态敌人和掉落实例。
    /// </summary>
    public void Configure(
        PlayerController player,
        PlayerHealth health,
        AutoShooter shooter,
        EnemySpawner enemySpawner,
        PickupSpawner pickupSpawner)
    {
        UnsubscribeGameplayEvents();
        _player = player;
        _health = health;
        _shooter = shooter;
        _enemySpawner = enemySpawner;
        _pickupSpawner = pickupSpawner;
        _shooter.VolleyFired += OnVolleyFired;
        _enemySpawner.EnemyDamaged += OnEnemyDamaged;
        _enemySpawner.EnemyDefeated += OnEnemyDefeated;
        _enemySpawner.EnemyExploded += OnEnemyExploded;
        _pickupSpawner.PickupCollected += OnPickupCollected;
        _health.Damaged += OnPlayerDamaged;
        _health.Died += OnPlayerDied;
    }

    /// <summary>
    /// 推进高射速音效限频，并根据玩家真实移动状态连续启停示例脚步素材。
    /// </summary>
    public override void _Process(double delta)
    {
        _shotSoundCooldown = Math.Max(0.0, _shotSoundCooldown - delta);
        bool shouldPlayFootstep = _player is not null && _health?.IsDead == false &&
            !_player.Velocity.IsZeroApprox();
        if (shouldPlayFootstep)
        {
            if (_footstep?.Playing == false)
            {
                _footstep.Play();
            }

            return;
        }

        if (_footstep?.Playing == true)
        {
            _footstep.Stop();
        }
    }

    /// <summary>
    /// 节点离开场景树前解除全部外部事件，防止重进游戏后旧控制器残留回调或重复播放。
    /// </summary>
    public override void _ExitTree()
    {
        StopAllPlayers();
        UnsubscribeGameplayEvents();
    }

    /// <summary>
    /// 停止控制器拥有的全部活动播放，确保返回菜单和程序退出时及时释放音频服务器句柄。
    /// </summary>
    private void StopAllPlayers()
    {
        foreach (Node child in GetChildren())
        {
            if (child is AudioStreamPlayer player && player.Playing)
            {
                player.Stop();
            }
        }
    }

    /// <summary>
    /// 为普通或螺旋弹幕的一整轮播放一次射击声，并限制极高射速下的声音密度。
    /// </summary>
    private void OnVolleyFired()
    {
        if (_shotSoundCooldown > 0.0)
        {
            return;
        }

        PlayOneShot(_shot);
        ShotSoundCount++;
        _shotSoundCooldown = MinimumShotSoundInterval;
    }

    /// <summary>
    /// 在任意掉落物成功应用强化后播放一次能力提升提示音。
    /// </summary>
    private void OnPickupCollected()
    {
        PlayOneShot(_pickup);
        PickupSoundCount++;
    }

    /// <summary>
    /// 在敌人承受非致命伤害时播放受击声，致命一击只交给死亡声处理。
    /// </summary>
    private void OnEnemyDamaged()
    {
        PlayOneShot(_enemyHit);
        EnemyHitSoundCount++;
    }

    /// <summary>
    /// 在敌人进入死亡流程时播放一次死亡音效，事件附带的位置和定义不参与全局混音。
    /// </summary>
    private void OnEnemyDefeated(Vector2 position, EnemyDefinition definition)
    {
        PlayOneShot(_enemyDeath);
        EnemyDeathSoundCount++;
    }

    /// <summary>
    /// 在自爆敌人正式切换到爆炸动画时播放爆炸声，避免声音早于视觉表现。
    /// </summary>
    private void OnEnemyExploded() => PlayOneShot(_explosion);

    /// <summary>
    /// 玩家承受非致命伤害时播放受击提示音，并保留独立计数供集成验证观察。
    /// </summary>
    private void OnPlayerDamaged()
    {
        PlayOneShot(_playerHurt);
        PlayerHurtSoundCount++;
    }

    /// <summary>
    /// 玩家生命归零时停止脚步和 BGM，再播放示例游戏提供的死亡音效。
    /// </summary>
    private void OnPlayerDied()
    {
        _footstep?.Stop();
        _bgm?.Stop();
        PlayOneShot(_playerDeath);
        PlayerDeathSoundCount++;
    }

    /// <summary>
    /// 对已配置流的播放器触发一次播放，由节点最大复音数统一限制重叠实例数量。
    /// </summary>
    private static void PlayOneShot(AudioStreamPlayer? player)
    {
        if (player?.Stream is not null)
        {
            player.Play();
        }
    }

    /// <summary>
    /// 从当前玩法依赖解除事件订阅；尚未配置或已部分释放时逐项安全跳过。
    /// </summary>
    private void UnsubscribeGameplayEvents()
    {
        if (_shooter is not null)
        {
            _shooter.VolleyFired -= OnVolleyFired;
        }

        if (_enemySpawner is not null)
        {
            _enemySpawner.EnemyDamaged -= OnEnemyDamaged;
            _enemySpawner.EnemyDefeated -= OnEnemyDefeated;
            _enemySpawner.EnemyExploded -= OnEnemyExploded;
        }

        if (_pickupSpawner is not null)
        {
            _pickupSpawner.PickupCollected -= OnPickupCollected;
        }

        if (_health is not null)
        {
            _health.Damaged -= OnPlayerDamaged;
            _health.Died -= OnPlayerDied;
        }
    }
}
