using AvaInstaller.Models;

namespace AvaInstaller.Services;

public interface IPayloadExtractor
{
    Task ExtractAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken);
}
