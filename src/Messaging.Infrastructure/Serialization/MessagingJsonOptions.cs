namespace Messaging.Infrastructure.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class MessagingJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };
}
