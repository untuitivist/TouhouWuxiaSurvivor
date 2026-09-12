namespace Rebirth.Core;

public sealed record BossCharacterProfile(float Health, float Speed, float Mass, float Radius, float ContactDamage);

public sealed record CharacterDefinition(string CharacterId, HeroDefinition Playable, BossCharacterProfile Boss)
{
    public HeroKind Id => Playable.Id;
    public string Name => Playable.Name;
}

public static class CharacterCatalog
{
    public static IReadOnlyList<CharacterDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new CharacterDefinition("character_base_00", new(HeroKind.Reimu, "博丽灵梦", 110, 1, 205,
            Array.AsReadOnly(new[] { ArtKind.Ofuda })), new(36000, 88, EnemyMassCatalog.Boss, 32, 20)),
        new CharacterDefinition("character_th02_soew_02", new(HeroKind.Marisa, "雾雨魔理沙", 100, 1, 220,
            Array.AsReadOnly(new[] { ArtKind.Stars })), new(34000, 96, EnemyMassCatalog.Boss, 32, 20))
    });

    public static CharacterDefinition Get(HeroKind hero)
        => hero switch { HeroKind.Reimu => All[0], HeroKind.Marisa => All[1], _ => throw new ArgumentOutOfRangeException(nameof(hero)) };

    public static IReadOnlyList<CharacterDefinition> BossCandidates(HeroKind selected, IEnumerable<HeroKind>? enabled = null)
    {
        var identity = Get(selected).CharacterId;
        var available = enabled?.ToHashSet();
        return All.Where(character => character.CharacterId != identity && (available == null || available.Contains(character.Id))).ToArray();
    }

    public static float PlayerMass(HeroKind hero) => hero == HeroKind.Reimu ? 65 : 60;
}
