using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Installer.Models;
using Installer.Services;

namespace Installer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string AppExecutableName = "MyAvaloniaApp.exe";
    private readonly IPayloadExtractor _payloadExtractor;
    private readonly IFolderPickerService _folderPickerService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcomePage))]
    [NotifyPropertyChangedFor(nameof(IsDirectoryPage))]
    [NotifyPropertyChangedFor(nameof(IsProgressPage))]
    [NotifyPropertyChangedFor(nameof(IsCompletePage))]
    [NotifyPropertyChangedFor(nameof(IsNotCompletePage))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private InstallPage currentPage = InstallPage.Welcome;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string installDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyAvaloniaApp");

    [ObservableProperty]
    private int progressPercent;

    [ObservableProperty]
    private string currentFile = string.Empty;

    [ObservableProperty]
    private string statusText = "Ready to install MyAvaloniaApp.";

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    private bool isInstalling;

    [ObservableProperty]
    private bool launchAfterInstall = true;

    public MainWindowViewModel()
        : this(new PayloadExtractor(), new FolderPickerService())
    {
    }

    public MainWindowViewModel(IPayloadExtractor payloadExtractor, IFolderPickerService folderPickerService)
    {
        _payloadExtractor = payloadExtractor;
        _folderPickerService = folderPickerService;
    }

    public bool IsWelcomePage => CurrentPage == InstallPage.Welcome;
    public bool IsDirectoryPage => CurrentPage == InstallPage.Directory;
    public bool IsProgressPage => CurrentPage == InstallPage.Progress;
    public bool IsCompletePage => CurrentPage == InstallPage.Complete;
    public bool IsNotCompletePage => CurrentPage != InstallPage.Complete;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private async Task BrowseAsync()
    {
        var selectedFolder = await _folderPickerService.PickFolderAsync(InstallDirectory, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            InstallDirectory = selectedFolder;
            ErrorMessage = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        ErrorMessage = null;

        CurrentPage = CurrentPage switch
        {
            InstallPage.Directory => InstallPage.Welcome,
            InstallPage.Progress => InstallPage.Directory,
            _ => CurrentPage
        };
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        ErrorMessage = null;

        CurrentPage = CurrentPage switch
        {
            InstallPage.Welcome => InstallPage.Directory,
            _ => CurrentPage
        };
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        ErrorMessage = null;
        CurrentPage = InstallPage.Progress;
        IsInstalling = true;
        ProgressPercent = 0;
        CurrentFile = string.Empty;
        StatusText = "Preparing installation...";

        try
        {
            var target = Path.GetFullPath(Environment.ExpandEnvironmentVariables(InstallDirectory.Trim()));
            InstallDirectory = target;

            var progress = new Progress<InstallProgress>(OnInstallProgressChanged);
            await _payloadExtractor.ExtractAsync(target, progress, CancellationToken.None);

            StatusText = "Installation completed.";
            CurrentFile = string.Empty;
            ProgressPercent = 100;
            CurrentPage = InstallPage.Complete;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Installation was canceled.";
            StatusText = "Installation canceled.";
            CurrentPage = InstallPage.Directory;
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = $"No permission to write to the selected folder. {ex.Message}";
            StatusText = "Installation failed.";
            CurrentPage = InstallPage.Directory;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or NotSupportedException)
        {
            ErrorMessage = ex.Message;
            StatusText = "Installation failed.";
            CurrentPage = InstallPage.Directory;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    [RelayCommand]
    private void Finish()
    {
        if (LaunchAfterInstall)
        {
            TryLaunchInstalledApp();
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void OnInstallProgressChanged(InstallProgress progress)
    {
        ProgressPercent = progress.Percent;
        CurrentFile = progress.CurrentFile;
        StatusText = string.IsNullOrWhiteSpace(progress.CurrentFile)
            ? "Extracting files..."
            : $"Extracting {progress.CurrentFile}";
    }

    private bool CanBrowse()
    {
        return !IsInstalling;
    }

    private bool CanGoBack()
    {
        return !IsInstalling && CurrentPage is InstallPage.Directory or InstallPage.Progress;
    }

    private bool CanGoNext()
    {
        return !IsInstalling && CurrentPage == InstallPage.Welcome;
    }

    private bool CanInstall()
    {
        return !IsInstalling &&
               CurrentPage == InstallPage.Directory &&
               !string.IsNullOrWhiteSpace(InstallDirectory);
    }

    private void TryLaunchInstalledApp()
    {
        try
        {
            var appPath = Path.Combine(InstallDirectory, AppExecutableName);
            if (!File.Exists(appPath))
            {
                ErrorMessage = $"Installed application was not found: {appPath}";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                WorkingDirectory = InstallDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = $"Unable to start {AppExecutableName}. {ex.Message}";
        }
    }
}
