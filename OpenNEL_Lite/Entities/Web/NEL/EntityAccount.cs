using System.Text.Json.Serialization;

namespace OpenNEL_Lite.Entities.Web.NEL;

public class EntityAccount
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("alias")]
	public required string Alias { get; set; }
}
