using System.Text.Json.Serialization;

namespace OpenNEL_Lite.Entities.Web.NEL;

public class EntityModifyAddress
{
	[JsonPropertyName("interceptor_id")]
	public string InterceptorId { get; set; } = string.Empty;

	[JsonPropertyName("ip")]
	public string IpAddress { get; set; } = string.Empty;

	[JsonPropertyName("port")]
	public string Port { get; set; } = string.Empty;
}
