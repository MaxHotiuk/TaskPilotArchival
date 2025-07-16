using System.Text.Json;

namespace Infrastructure.Helpers;

public static class SerializationHelper
{
    public static string SerializeToJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
    public static T DeserializeFromJson<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream) ?? throw new InvalidOperationException("Deserialization returned null");
    }
}
