namespace Rebirth.Core;

public static class EnemyMassCatalog
{
    public const float Kedama = 12;
    public const float Fairy = 20;
    public const float Charger = 40;
    public const float Elite = 80;
    public const float Boss = 260;

    public static float Get(EnemyKind kind) => kind switch
    {
        EnemyKind.Kedama => Kedama,
        EnemyKind.Fairy => Fairy,
        EnemyKind.Charger => Charger,
        EnemyKind.Elite => Elite,
        EnemyKind.Boss => Boss,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
