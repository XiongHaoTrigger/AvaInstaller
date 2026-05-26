namespace Installer.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(string suggestedPath, CancellationToken cancellationToken);
}
