using System.Text.Json;
using AvaInstaller.Models;
using AvaInstaller.Models.Json;

namespace AvaInstaller.Services;

public sealed class InstallManifestService
{
    public string GetManifestPath(string installLocation)
    {
        return Path.Combine(installLocation, InstallerMetadata.ManifestFileName);
    }

    public async Task WriteAsync(InstallManifest manifest, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(manifest.InstallLocation);
        var path = GetManifestPath(manifest.InstallLocation);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            manifest,
            InstallerJsonContext.Default.InstallManifest,
            cancellationToken);
    }

    public async Task<InstallManifest> ReadAsync(string manifestPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync(
            stream,
            InstallerJsonContext.Default.InstallManifest,
            cancellationToken);

        return manifest ?? throw new InvalidDataException($"Manifest is empty: {manifestPath}");
    }
}
