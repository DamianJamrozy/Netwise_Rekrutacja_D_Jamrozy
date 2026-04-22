using System.Text.Json;

namespace Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Serialization;

public static class JsonLineSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static T? Deserialize<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value, SerializerOptions);
    }
}