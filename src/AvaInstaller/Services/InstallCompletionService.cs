using AvaInstaller.Models;

namespace AvaInstaller.Services;

public sealed class InstallCompletionService
{
    private static readonly string[] UninstallerSupportFiles =
    [
        "av_libglesv2.dll",
        "libHarfBuzzSharp.dll",
        "libSkiaSharp.dll"
    ];

    private readonly InstallManifestService _manifestService;
    private readonly InstallStateService _stateService;

    public InstallCompletionService(
        InstallManifestService manifestService,
        InstallStateService stateService)
    {
        _manifestService = manifestService;
        _stateService = stateService;
    }

    public async Task CompleteAsync(
        string installLocation,
        PayloadExtractionResult extractionResult,
        CancellationToken cancellationToken)
    {
        var installedAt = DateTimeOffset.Now;
        var files = extractionResult.Files.ToList();
        var directories = extractionResult.Directories.ToList();

        CopyUninstaller(installLocation, files);
        AddIfMissing(files, InstallerMetadata.Uninstaller);

        var manifestPath = _manifestService.GetManifestPath(installLocation);

        var manifest = new InstallManifest
        {
            AppName = InstallerMetadata.AppName,
            AppId = InstallerMetadata.AppId,
            Version = InstallerMetadata.Version,
            Publisher = InstallerMetadata.Publisher,
            InstallLocation = installLocation,
            MainExe = InstallerMetadata.MainExe,
            Uninstaller = InstallerMetadata.Uninstaller,
            InstalledAt = installedAt,
            Files = files.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Directories = directories.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Shortcuts = [],
            Preserve = ["user.config", "logs"]
        };

        await _manifestService.WriteAsync(manifest, cancellationToken);

        var state = new InstallState
        {
            AppName = manifest.AppName,
            AppId = manifest.AppId,
            Version = manifest.Version,
            InstallLocation = manifest.InstallLocation,
            ManifestPath = manifestPath,
            InstalledAt = manifest.InstalledAt,
            MainExe = manifest.MainExe,
            Uninstaller = manifest.Uninstaller
        };

        await _stateService.WriteAsync(state, cancellationToken);
    }

    private static void CopyUninstaller(string installLocation, List<string> files)
    {
        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
        {
            throw new FileNotFoundException("Unable to locate current installer executable.");
        }

        var uninstallerPath = Path.Combine(installLocation, InstallerMetadata.Uninstaller);
        if (Path.GetFullPath(currentExecutable).Equals(Path.GetFullPath(uninstallerPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Copy(currentExecutable, uninstallerPath, overwrite: true);

        var sourceDirectory = AppContext.BaseDirectory;
        foreach (var supportFile in UninstallerSupportFiles)
        {
            var sourcePath = Path.Combine(sourceDirectory, supportFile);
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var destinationPath = Path.Combine(installLocation, supportFile);
            if (!Path.GetFullPath(sourcePath).Equals(Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }

            AddIfMissing(files, supportFile);
        }
    }

    private static void AddIfMissing(List<string> items, string value)
    {
        if (!items.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(value);
        }
    }
}
