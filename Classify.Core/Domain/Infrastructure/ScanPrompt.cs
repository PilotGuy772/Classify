namespace Classify.Core.Domain.Infrastructure;

public class ScanPrompt(string message)
{
    public string Message { get; } = message;
}