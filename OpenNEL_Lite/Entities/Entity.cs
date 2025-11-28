using System;
using System.Text.Json.Serialization;

namespace OpenNEL_Lite.Entities;

public class Entity
{
	[JsonPropertyName("identify")]
	public string? Identify { get; set; }

	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("payload")]
	public string? Payload { get; set; }

	[JsonPropertyName("sign")]
	public string? Sign { get; set; }

	public Entity()
	{
	}

	public Entity(string type, string payload)
	{
		Type = type ?? throw new ArgumentNullException("type");
		Payload = payload ?? throw new ArgumentNullException("payload");
	}

}
