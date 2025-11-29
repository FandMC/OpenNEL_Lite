using System.Text.Json.Serialization;
using OpenNEL_Lite.Enums;

namespace OpenNEL_Lite.Entities.Web.NEL;

public class EntityUpdateUserAlias
{
	[JsonPropertyName("id")]
	public required string EntityId { get; set; }

	[JsonPropertyName("platform")]
	public required Platform Platform { get; set; }

	[JsonPropertyName("alias")]
	public required string Alias { get; set; }
}
