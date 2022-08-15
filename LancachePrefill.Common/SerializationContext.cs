using BattleNetPrefill.Utils;

namespace BattleNetPrefill.Structs.Enums
{
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(List<GithubRelease>))]
    internal partial class SerializationContext : JsonSerializerContext
    {
    }
}
