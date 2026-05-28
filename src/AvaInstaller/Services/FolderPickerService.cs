using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AvaInstaller.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    // Avalonia 的文件夹选择器需要从当前主窗口获取 StorageProvider。
    public async Task<string?> PickFolderAsync(string suggestedPath, CancellationToken cancellationToken)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window window)
        {
            return null;
        }

        var storageProvider = window.StorageProvider;
        if (!storageProvider.CanOpen)
        {
            return null;
        }

        IStorageFolder? suggestedFolder = null;
        if (!string.IsNullOrWhiteSpace(suggestedPath) && Directory.Exists(suggestedPath))
        {
            suggestedFolder = await storageProvider.TryGetFolderFromPathAsync(suggestedPath);
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select installation folder",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedFolder
        });

        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
