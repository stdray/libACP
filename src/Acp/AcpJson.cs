using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Acp.Schema.Converters;

namespace Acp;

/// <summary>
/// Centralized <see cref="JsonSerializerOptions"/> used for all ACP serialization.
/// </summary>
public static class AcpJson
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used by ACP. Configured for camelCase property naming,
    /// null-skipping on write, and all custom converters needed by the protocol's discriminated
    /// unions and primitive wrappers.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.Strict,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        options.Converters.Add(new RequestIdJsonConverter());
        options.Converters.Add(new ContentBlockJsonConverter());
        options.Converters.Add(new ToolCallContentJsonConverter());
        options.Converters.Add(new EmbeddedResourceResourceJsonConverter());
        options.Converters.Add(new McpServerJsonConverter());
        options.Converters.Add(new SessionUpdateJsonConverter());
        options.Converters.Add(new RequestPermissionOutcomeJsonConverter());
        options.Converters.Add(new ElicitationPropertySchemaJsonConverter());
        options.Converters.Add(new MultiSelectItemsJsonConverter());
        options.Converters.Add(new ElicitationContentValueJsonConverter());
        var elicitationRequest = new CreateElicitationRequestJsonConverter();
        var elicitationResponse = new CreateElicitationResponseJsonConverter();
        options.Converters.Add(elicitationRequest);
        options.Converters.Add(elicitationResponse);
        options.Converters.Add(new DerivedUnionJsonConverterFactory<Schema.CreateElicitationRequest>(elicitationRequest));
        options.Converters.Add(new DerivedUnionJsonConverterFactory<Schema.CreateElicitationResponse>(elicitationResponse));

        options.MakeReadOnly();
        return options;
    }
}
