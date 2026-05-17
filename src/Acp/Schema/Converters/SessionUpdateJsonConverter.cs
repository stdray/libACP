using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>Polymorphic converter for <see cref="SessionUpdate"/>. Discriminated by <c>sessionUpdate</c>.</summary>
public sealed class SessionUpdateJsonConverter : JsonConverter<SessionUpdate>
{
    public override SessionUpdate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(SessionUpdate));
        string disc = DiscriminatedUnionHelper.ReadDiscriminator(el, "sessionUpdate", nameof(SessionUpdate));
        return disc switch
        {
            "user_message_chunk" => el.Deserialize<UserMessageChunk>(options)!,
            "agent_message_chunk" => el.Deserialize<AgentMessageChunk>(options)!,
            "agent_thought_chunk" => el.Deserialize<AgentThoughtChunk>(options)!,
            "tool_call" => el.Deserialize<ToolCallStartUpdate>(options)!,
            "tool_call_update" => el.Deserialize<ToolCallUpdateUpdate>(options)!,
            "plan" => el.Deserialize<PlanUpdate>(options)!,
            "available_commands_update" => el.Deserialize<AvailableCommandsUpdate>(options)!,
            "current_mode_update" => el.Deserialize<CurrentModeUpdate>(options)!,
            "session_info_update" => el.Deserialize<SessionInfoUpdate>(options)!,
            "end_turn" => el.Deserialize<EndTurnUpdate>(options)!,
            "diff" => el.Deserialize<DiffUpdate>(options)!,
            _ => throw new JsonException($"Unknown SessionUpdate kind '{disc}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, SessionUpdate value, JsonSerializerOptions options)
    {
        JsonElement obj = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        writer.WriteStartObject();
        writer.WriteString("sessionUpdate", value.SessionUpdateKind);
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, "sessionUpdate", StringComparison.Ordinal)) continue;
            if (string.Equals(prop.Name, "sessionUpdateKind", StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
