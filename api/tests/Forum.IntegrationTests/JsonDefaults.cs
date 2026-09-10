using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forum.IntegrationTests;

public static class JsonDefaults
{
    /// <summary>
    /// Mirrors the API's serializer settings. Enums travel as strings on the wire, so a
    /// client reading them back needs the same converter — exactly as the Angular client
    /// treats <c>role</c> as a string union rather than a number.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
