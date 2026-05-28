namespace AvaInstaller.Models;

// PayloadExtractor 返回的安装产物清单，用于后续生成 install-manifest.json。
public sealed record PayloadExtractionResult(
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Directories);
