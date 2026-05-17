using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using Acp.JsonRpc;

namespace Acp.Streaming;

/// <summary>
/// Newline-delimited JSON framing over a duplex pair of <see cref="System.IO.Stream"/>s. This is
/// the standard ACP transport when running an agent as a stdio subprocess.
/// </summary>
/// <remarks>
/// <para>
/// Messages are individual JSON-RPC values, separated by <c>\n</c>. They are UTF-8 encoded and
/// MUST NOT contain embedded newlines. The reader is robust to Windows-style <c>\r\n</c> line
/// endings (the <c>\r</c> is trimmed before parsing).
/// </para>
/// <para>
/// The reader and writer use independent <see cref="SemaphoreSlim"/>s, so concurrent reads and
/// writes are safe. Concurrent writes are serialized in the order they enter
/// <see cref="WriteAsync"/>.
/// </para>
/// </remarks>
public sealed class NdJsonStream : IMessageStream
{
    /// <summary>Default upper bound on a single message line, in bytes.</summary>
    public const int DefaultMaxLineLength = 16 * 1024 * 1024; // 16 MiB

    private readonly PipeReader _reader;
    private readonly Stream _output;
    private readonly bool _ownsInput;
    private readonly bool _ownsOutput;
    private readonly Stream? _input;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly int _maxLineLength;
    private bool _disposed;

    /// <summary>
    /// Create an ndjson stream over the given input/output streams. The streams will be disposed
    /// alongside this object when <paramref name="leaveOpen"/> is <c>false</c>.
    /// </summary>
    public NdJsonStream(Stream input, Stream output, bool leaveOpen = true, int maxLineLength = DefaultMaxLineLength)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        if (!input.CanRead) throw new ArgumentException("Input stream must be readable.", nameof(input));
        if (!output.CanWrite) throw new ArgumentException("Output stream must be writable.", nameof(output));
        if (maxLineLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLineLength));

        _input = input;
        _output = output;
        _ownsInput = !leaveOpen;
        _ownsOutput = !leaveOpen;
        _maxLineLength = maxLineLength;
        _reader = PipeReader.Create(input, new StreamPipeReaderOptions(leaveOpen: true));
    }

    /// <summary>
    /// Convenience factory bound to the current process's standard input and standard output. The
    /// console is not closed on dispose.
    /// </summary>
    public static NdJsonStream FromStdio(int maxLineLength = DefaultMaxLineLength) =>
        new(Console.OpenStandardInput(), Console.OpenStandardOutput(), leaveOpen: true, maxLineLength);

    public async Task<JsonRpcMessage?> ReadAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            ReadResult result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;
            SequencePosition consumed = buffer.Start;
            SequencePosition examined = buffer.End;

            try
            {
                if (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    // We consumed one full line; we have NOT looked at anything past it yet, so set
                    // examined = consumed so the next ReadAsync returns immediately if there are
                    // more buffered messages waiting.
                    consumed = buffer.Start;
                    examined = buffer.Start;

                    JsonRpcMessage? msg = ParseLine(line);
                    if (msg is null) continue; // empty / whitespace-only line
                    return msg;
                }

                if (result.IsCompleted)
                {
                    if (!buffer.IsEmpty)
                    {
                        // Trailing partial message without a terminating newline.
                        JsonRpcMessage? msg = ParseLine(buffer);
                        consumed = buffer.End;
                        examined = buffer.End;
                        if (msg is not null) return msg;
                    }
                    return null;
                }

                if (buffer.Length > _maxLineLength)
                {
                    throw RequestErrorException.ParseError(
                        new { maxLineLength = _maxLineLength, observedBytes = buffer.Length },
                        $"Message exceeds {_maxLineLength} bytes without a newline.");
                }
            }
            finally
            {
                _reader.AdvanceTo(consumed, examined);
            }
        }
    }

    public async Task WriteAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        byte[] payload;
        try
        {
            using var ms = new MemoryStream(256);
            using (var writer = new Utf8JsonWriter(ms))
            {
                JsonSerializer.Serialize(writer, message, AcpJson.Options);
            }
            ms.WriteByte((byte)'\n');
            payload = ms.ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to serialize JSON-RPC message.", ex);
        }

        if (Array.IndexOf(payload, (byte)'\n', 0, payload.Length - 1) >= 0)
        {
            throw new InvalidOperationException(
                "Serialized JSON-RPC message contains an embedded newline; ndjson framing requires single-line messages.");
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _output.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _reader.CompleteAsync().ConfigureAwait(false);
        if (_ownsInput && _input is not null) await _input.DisposeAsync().ConfigureAwait(false);
        if (_ownsOutput) await _output.DisposeAsync().ConfigureAwait(false);
        _writeLock.Dispose();
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        SequencePosition? pos = buffer.PositionOf((byte)'\n');
        if (pos is null)
        {
            line = default;
            return false;
        }
        line = buffer.Slice(0, pos.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));
        return true;
    }

    private static JsonRpcMessage? ParseLine(ReadOnlySequence<byte> line)
    {
        // Trim trailing \r if present (Windows line endings).
        if (!line.IsEmpty)
        {
            byte last = line.IsSingleSegment
                ? line.FirstSpan[^1]
                : SegmentLast(line);
            if (last == (byte)'\r')
            {
                line = line.Slice(0, line.Length - 1);
            }
        }

        if (line.IsEmpty) return null;
        if (IsAllWhitespace(line)) return null;

        try
        {
            var reader = new Utf8JsonReader(line);
            JsonElement element = JsonElement.ParseValue(ref reader);
            return JsonRpcMessageReader.Read(element);
        }
        catch (JsonException ex)
        {
            string snippet = SafeSnippet(line);
            throw RequestErrorException.ParseError(new { snippet }, ex.Message);
        }

        static byte SegmentLast(ReadOnlySequence<byte> seq)
        {
            byte b = 0;
            foreach (var seg in seq)
            {
                if (!seg.IsEmpty) b = seg.Span[^1];
            }
            return b;
        }

        static bool IsAllWhitespace(ReadOnlySequence<byte> seq)
        {
            foreach (var seg in seq)
            {
                foreach (byte b in seg.Span)
                {
                    if (b is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')) return false;
                }
            }
            return true;
        }

        static string SafeSnippet(ReadOnlySequence<byte> seq)
        {
            const int max = 200;
            int len = (int)Math.Min(seq.Length, max);
            byte[] buf = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                seq.Slice(0, len).CopyTo(buf);
                return Encoding.UTF8.GetString(buf, 0, len);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}

internal static class JsonRpcMessageReader
{
    public static JsonRpcMessage Read(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw RequestErrorException.InvalidRequest(additionalMessage: $"expected JSON object, got {element.ValueKind}.");
        }

        string jsonRpc = "2.0";
        string? method = null;
        bool hasId = false;
        RequestId id = default;
        JsonElement? @params = null;
        JsonElement? result = null;
        JsonRpcError? error = null;

        foreach (JsonProperty prop in element.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "jsonrpc":
                    if (prop.Value.ValueKind != JsonValueKind.String) goto invalid;
                    jsonRpc = prop.Value.GetString()!;
                    break;
                case "method":
                    if (prop.Value.ValueKind != JsonValueKind.String) goto invalid;
                    method = prop.Value.GetString();
                    break;
                case "id":
                    hasId = true;
                    id = ReadId(prop.Value);
                    break;
                case "params":
                    @params = prop.Value.Clone();
                    break;
                case "result":
                    result = prop.Value.Clone();
                    break;
                case "error":
                    error = ReadError(prop.Value);
                    break;
                default:
                    // Unknown top-level fields are ignored (extensibility).
                    break;
            }
        }

        if (!string.Equals(jsonRpc, "2.0", StringComparison.Ordinal))
        {
            throw RequestErrorException.InvalidRequest(additionalMessage: $"unsupported jsonrpc version '{jsonRpc}'.");
        }

        return new JsonRpcMessage
        {
            JsonRpc = jsonRpc,
            Method = method,
            Id = id,
            HasId = hasId,
            Params = @params,
            Result = result,
            Error = error,
        };

    invalid:
        throw RequestErrorException.InvalidRequest(additionalMessage: "malformed envelope.");
    }

    private static RequestId ReadId(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetInt64(out long n) => RequestId.FromNumber(n),
        JsonValueKind.String => RequestId.FromString(value.GetString()!),
        JsonValueKind.Null => RequestId.Null(),
        _ => throw RequestErrorException.InvalidRequest(additionalMessage: $"id must be string, integer, or null (got {value.ValueKind})."),
    };

    private static JsonRpcError ReadError(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw RequestErrorException.InvalidRequest(additionalMessage: "error must be an object.");
        }
        int code = value.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out int ci)
            ? ci : throw RequestErrorException.InvalidRequest(additionalMessage: "error.code must be an integer.");
        string message = value.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
            ? m.GetString()! : throw RequestErrorException.InvalidRequest(additionalMessage: "error.message must be a string.");
        JsonElement? data = value.TryGetProperty("data", out var d) ? d.Clone() : null;
        return new JsonRpcError { Code = code, Message = message, Data = data };
    }
}
