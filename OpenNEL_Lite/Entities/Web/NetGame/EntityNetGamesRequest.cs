using System.Text.Json.Serialization;

namespace OpenNEL_Lite.Entities.Web.NetGame;

public class EntityNetGamesRequest
{
	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("length")]
	public int Length { get; set; }
}
