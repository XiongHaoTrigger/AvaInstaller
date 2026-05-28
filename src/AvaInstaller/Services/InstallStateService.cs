using System.Text.Json;
using AvaInstaller.Models;
using AvaInstaller.Models.Json;

namespace AvaInstaller.Services;

public sealed class InstallStateService
{
    public string GetStatePath(string appId = InstallerMetadata.AppId)
    {
        return InstallPathService.GetInstallStatePath(appId);
    }

    public async Task<InstallState?> TryReadAsync(CancellationToken cancellationToken)
    {
        var path = GetStatePath();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(
                stream,
                InstallerJsonContext.Default.InstallState,
                cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public async Task WriteAsync(InstallState state, CancellationToken cancellationToken)
    {
        var path = GetStatePath(state.AppId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            state,
            InstallerJsonContext.Default.InstallState,
            cancellationToken);
    }

    public void Delete(string appId = InstallerMetadata.AppId)
    {
        var path = GetStatePath(appId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) &&
            Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
