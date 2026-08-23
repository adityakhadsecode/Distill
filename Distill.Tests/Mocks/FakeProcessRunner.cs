using Distill.Core.Process;

namespace Distill.Tests.Mocks;

/// <summary>
/// Mock implementation of <see cref="IProcessRunner"/> for unit testing without spawning real OS processes.
/// </summary>
public class FakeProcessRunner : IProcessRunner
{
    public record ExecutionRecord(string ExecutablePath, string Arguments, string? WorkingDirectory);

    public List<ExecutionRecord> Executions { get; } = [];

    public Func<string, string, string?, ProcessResult>? CustomHandler { get; set; }

    public Task<ProcessResult> RunAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var record = new ExecutionRecord(executablePath, arguments, workingDirectory);
        Executions.Add(record);

        if (CustomHandler != null)
        {
            return Task.FromResult(CustomHandler(executablePath, arguments, workingDirectory));
        }

        return Task.FromResult(new ProcessResult(0, "OK", string.Empty));
    }
}

public class FakeToolLocator : IToolLocator
{
    public string ResolveToolPath(string toolFileName) => toolFileName;
}
