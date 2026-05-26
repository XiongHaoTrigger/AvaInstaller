using Installer.Models;

namespace Installer.Services;

public interface IPayloadExtractor
{
    Task ExtractAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken);
}
