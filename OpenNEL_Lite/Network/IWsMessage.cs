using System.Text.Json;

namespace OpenNEL_Lite.Network;

internal interface IWsMessage
{
    string Type { get; }
    Task<object?> ProcessAsync(JsonElement root);
}
