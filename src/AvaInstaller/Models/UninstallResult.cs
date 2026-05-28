namespace AvaInstaller.Models;

public sealed record UninstallResult(bool Succeeded, string? ErrorMessage, string? LogPath);
