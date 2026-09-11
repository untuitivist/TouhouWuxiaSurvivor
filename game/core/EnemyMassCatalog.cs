namespace Rebirth.Core;

public static class EnemyMassCatalog
{
    public const float Elite = 80;
    public const float Boss = 260;

    public static float Get(EnemyKind kind) => kind switch
    {
        EnemyKind.Kedama => 12,
        EnemyKind.Fairy => 20,
        EnemyKind.Charger => 40,
        EnemyKind.Elite => Elite,
        EnemyKind.Boss => Boss,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
