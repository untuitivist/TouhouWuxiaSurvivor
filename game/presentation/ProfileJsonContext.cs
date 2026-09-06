using System.Text.Json.Serialization;

namespace Rebirth.Presentation;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PlayerProfile))]
[JsonSerializable(typeof(WebCheckState))]
internal partial class ProfileJsonContext : JsonSerializerContext;
