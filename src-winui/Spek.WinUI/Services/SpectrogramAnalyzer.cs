using Spek.WinUI.Models;

namespace Spek.WinUI.Services;

public sealed class SpectrogramAnalyzer
{
    private readonly ManagedSpectrogramAnalyzer managedAnalyzer = new();
    private NativeSpectrogramAnalyzer? nativeAnalyzer = NativeSpectrogramAnalyzer.TryCreate();

    public async Task<SpectrogramResult> AnalyzeAsync(
        string path,
        SpectrogramOptions options,
        int targetColumns,
        CancellationToken cancellationToken)
    {
        if (nativeAnalyzer is not null)
        {
            try
            {
                return await nativeAnalyzer.AnalyzeAsync(path, options, targetColumns, cancellationToken);
            }
            catch (NativeAnalyzerUnavailableException)
            {
                nativeAnalyzer = null;
            }
        }

        return await managedAnalyzer.AnalyzeAsync(path, options, targetColumns, cancellationToken);
    }
}
