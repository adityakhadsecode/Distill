namespace Distill.Core.Process;

/// <summary>
/// Execution output from a spawned process.
/// </summary>
/// <param name="ExitCode">Process exit code (0 indicates success).</param>
/// <param name="StandardOutput">Captured standard output stream.</param>
/// <param name="StandardError">Captured standard error stream.</param>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Abstraction for executing external command-line processes asynchronously.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Executes an external executable process with the specified arguments.
    /// </summary>
    /// <param name="executablePath">Absolute or resolved PATH executable.</param>
    /// <param name="arguments">CLI arguments.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ProcessResult"/> with output and exit code.</returns>
    Task<ProcessResult> RunAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}
