using System.Text.Json;

namespace Infrastructure.Helpers;

public static class SerializationHelper
{
    public static string SerializeToJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
}
