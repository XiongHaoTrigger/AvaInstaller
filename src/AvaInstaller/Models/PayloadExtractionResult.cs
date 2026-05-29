/// <summary>
/// Payload 解压结果记录。
/// PayloadExtractor 返回的安装产物清单，供 InstallCompletionService 生成 install-manifest.json。
/// </summary>
/// <param name="Files">解压出的所有文件相对路径列表</param>
/// <param name="Directories">解压出的所有目录相对路径列表</param>
namespace AvaInstaller.Models;

public sealed record PayloadExtractionResult(
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Directories);
