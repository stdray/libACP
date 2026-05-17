namespace Acp.Schema;

/// <summary>
/// The latest ACP protocol major version supported by this library.
/// </summary>
public static class Protocol
{
    /// <summary>The protocol version constant. Increments only on breaking changes.</summary>
    public const int Version = 1;
}

/// <summary>
/// Method names that can be invoked on an ACP Agent.
/// </summary>
public static class AgentMethods
{
    public const string Initialize = "initialize";
    public const string Authenticate = "authenticate";
    public const string SessionNew = "session/new";
    public const string SessionLoad = "session/load";
    public const string SessionResume = "session/resume";
    public const string SessionList = "session/list";
    public const string SessionClose = "session/close";
    public const string SessionPrompt = "session/prompt";
    public const string SessionSetMode = "session/set_mode";
    public const string SessionSetModel = "session/set_model";
    public const string SessionSetConfigOption = "session/set_config_option";

    /// <summary>Notification (no response).</summary>
    public const string SessionCancel = "session/cancel";
}

/// <summary>
/// Method names that can be invoked on an ACP Client.
/// </summary>
public static class ClientMethods
{
    public const string FsReadTextFile = "fs/read_text_file";
    public const string FsWriteTextFile = "fs/write_text_file";

    public const string SessionRequestPermission = "session/request_permission";

    public const string TerminalCreate = "terminal/create";
    public const string TerminalOutput = "terminal/output";
    public const string TerminalRelease = "terminal/release";
    public const string TerminalWaitForExit = "terminal/wait_for_exit";
    public const string TerminalKill = "terminal/kill";

    /// <summary>Notification (no response).</summary>
    public const string SessionUpdate = "session/update";
}
