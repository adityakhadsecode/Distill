using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Distill.Core.Process;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> using <see cref="System.Diagnostics.Process"/>.
/// </summary>
public class DefaultProcessRunner : IProcessRunner
{
    private readonly ILogger<DefaultProcessRunner>? _logger;

    public DefaultProcessRunner(ILogger<DefaultProcessRunner>? logger = null)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Spawning process: {Exe} {Args} in {Dir}", executablePath, arguments, workingDirectory ?? "current");

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore kill errors during cancellation
            }

            throw;
        }
    }
}
