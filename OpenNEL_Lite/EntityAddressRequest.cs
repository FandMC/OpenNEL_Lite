using System.Text.Json.Serialization;

namespace OpenNEL_Lite;

public class EntityAddressRequest
{
    [JsonPropertyName("item_id")] public string ItemId { get; set; } = string.Empty;
}