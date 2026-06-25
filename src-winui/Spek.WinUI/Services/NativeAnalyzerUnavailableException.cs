namespace Spek.WinUI.Services;

public sealed class NativeAnalyzerUnavailableException : Exception
{
    public NativeAnalyzerUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
