namespace BattleNetPrefill.Structs.Enums
{
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(List<Request>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(ConcurrentDictionary<string, long>))]
    internal partial class SerializationContext : JsonSerializerContext
    {
    }
}
