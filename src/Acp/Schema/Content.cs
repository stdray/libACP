using System.Text.Json.Serialization;
using Acp.Schema.Converters;

namespace Acp.Schema;

/// <summary>
/// Content blocks represent displayable information in the Agent Client Protocol. They appear in
/// user prompts, language-model output streamed via session updates, and tool call content.
/// </summary>
/// <remarks>
/// Discriminated by the <c>type</c> property; concrete variants are
/// <see cref="TextContent"/>, <see cref="ImageContent"/>, <see cref="AudioContent"/>,
/// <see cref="ResourceLinkContent"/>, and <see cref="EmbeddedResourceContent"/>.
/// </remarks>
[JsonConverter(typeof(ContentBlockJsonConverter))]
public abstract record ContentBlock
{
    [JsonPropertyName("annotations")]
    public Annotations? Annotations { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>The wire <c>type</c> discriminator value.</summary>
    [JsonIgnore]
    public abstract string Type { get; }
}

/// <summary>
/// Text content. May be plain text or formatted with Markdown. All agents MUST support text in
/// prompts.
/// </summary>
public sealed record TextContent : ContentBlock
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    public override string Type => "text";
}

/// <summary>Image content for visual context or analysis. Requires the <c>image</c> prompt capability.</summary>
public sealed record ImageContent : ContentBlock
{
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    public override string Type => "image";
}

/// <summary>Audio content for transcription or analysis. Requires the <c>audio</c> prompt capability.</summary>
public sealed record AudioContent : ContentBlock
{
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    public override string Type => "audio";
}

/// <summary>A reference to a resource the agent can access. All agents MUST support these in prompts.</summary>
public sealed record ResourceLinkContent : ContentBlock
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("size")]
    public long? Size { get; init; }

    public override string Type => "resource_link";
}

/// <summary>
/// Complete resource contents embedded directly in the message. Requires the <c>embeddedContext</c>
/// prompt capability when used in a prompt.
/// </summary>
public sealed record EmbeddedResourceContent : ContentBlock
{
    [JsonPropertyName("resource")]
    public required EmbeddedResourceResource Resource { get; init; }

    public override string Type => "resource";
}

/// <summary>
/// The contents of an embedded resource: either text or binary (blob).
/// </summary>
[JsonConverter(typeof(EmbeddedResourceResourceJsonConverter))]
public abstract record EmbeddedResourceResource
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Embedded text resource.</summary>
public sealed record TextResourceContents : EmbeddedResourceResource
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

/// <summary>Embedded binary (base64) resource.</summary>
public sealed record BlobResourceContents : EmbeddedResourceResource
{
    [JsonPropertyName("blob")]
    public required string Blob { get; init; }
}
