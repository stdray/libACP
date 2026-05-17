using System.IO.Pipelines;
using Acp.Streaming;

namespace Acp.Tests;

/// <summary>
/// Helper that wires two <see cref="System.IO.Pipelines.Pipe"/> instances into a duplex pair of
/// <see cref="NdJsonStream"/>s — one for each "side" of the protocol. Useful for exercising both
/// <see cref="AgentSideConnection"/> and <see cref="ClientSideConnection"/> in the same process
/// without spawning a subprocess or opening sockets.
/// </summary>
internal static class PairedNdJsonStreams
{
    public static (NdJsonStream agentSide, NdJsonStream clientSide) Create()
    {
        // pipe A: client -> agent (agent reads, client writes)
        var pipeA = new Pipe();
        // pipe B: agent -> client (client reads, agent writes)
        var pipeB = new Pipe();

        Stream agentInput = pipeA.Reader.AsStream(leaveOpen: false);
        Stream agentOutput = pipeB.Writer.AsStream(leaveOpen: false);
        Stream clientInput = pipeB.Reader.AsStream(leaveOpen: false);
        Stream clientOutput = pipeA.Writer.AsStream(leaveOpen: false);

        var agentStream = new NdJsonStream(agentInput, agentOutput, leaveOpen: false);
        var clientStream = new NdJsonStream(clientInput, clientOutput, leaveOpen: false);
        return (agentStream, clientStream);
    }
}
