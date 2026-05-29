using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AvaInstaller.Services;

/// <summary>
/// 文件夹选择器服务实现。
/// 使用 Avalonia 的 StorageProvider 打开原生系统文件夹选择对话框，
/// 需要从当前主窗口获取 StorageProvider 实例。
/// </summary>
public sealed class FolderPickerService : IFolderPickerService
{
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string suggestedPath, CancellationToken cancellationToken)
    {
        // Avalonia 的文件夹选择器需要从当前主窗口获取 StorageProvider
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

        // 尝试设置建议的初始目录
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
