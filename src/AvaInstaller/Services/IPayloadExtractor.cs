using AvaInstaller.Models;

namespace AvaInstaller.Services;

public interface IPayloadExtractor
{
    Task<PayloadExtractionResult> ExtractAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken);
}
