namespace Distill.Core.Exceptions;

/// <summary>
/// Base exception for AI distillation and formatting errors.
/// </summary>
public class DistillAiException : Exception
{
    public DistillAiException(string message) : base(message) { }
    public DistillAiException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when Distill cannot connect to the local Ollama instance.
/// </summary>
public class OllamaConnectionException : DistillAiException
{
    public string Endpoint { get; }

    public OllamaConnectionException(string endpoint, string message, Exception? innerException = null)
        : base($"Could not connect to local Ollama instance at '{endpoint}'.\n{message}\n" +
               "Please ensure Ollama is installed and running (run 'ollama serve' or start the Ollama desktop app).",
               innerException!)
    {
        Endpoint = endpoint;
    }
}
