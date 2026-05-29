using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 安装完成后处理服务。
/// 负责 Payload 解压后的收尾工作：
/// 1. 复制卸载程序（以及其依赖的运行时 DLL）到安装目录
/// 2. 生成安装清单文件（install-manifest.json）
/// 3. 写入安装状态文件（install-state.json）
/// </summary>
public sealed class InstallCompletionService
{
    /// <summary>
    /// 卸载程序运行所需的 Avalonia 运行时支持文件列表。
    /// 安装时需要从安装器目录一并复制到安装目录。
    /// </summary>
    private static readonly string[] UninstallerSupportFiles =
    [
        "av_libglesv2.dll",
        "libHarfBuzzSharp.dll",
        "libSkiaSharp.dll"
    ];

    private readonly InstallManifestService _manifestService;
    private readonly InstallStateService _stateService;

    /// <summary>
    /// 创建 InstallCompletionService 实例。
    /// </summary>
    /// <param name="manifestService">安装清单读写服务</param>
    /// <param name="stateService">安装状态读写服务</param>
    public InstallCompletionService(
        InstallManifestService manifestService,
        InstallStateService stateService)
    {
        _manifestService = manifestService;
        _stateService = stateService;
    }

    /// <summary>
    /// 执行安装后处理流程。
    /// </summary>
    /// <param name="installLocation">安装目标目录</param>
    /// <param name="extractionResult">Payload 解压结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CompleteAsync(
        string installLocation,
        PayloadExtractionResult extractionResult,
        CancellationToken cancellationToken)
    {
        var installedAt = DateTimeOffset.Now;
        var files = extractionResult.Files.ToList();
        var directories = extractionResult.Directories.ToList();

        // 复制卸载程序到安装目录
        CopyUninstaller(installLocation, files);
        AddIfMissing(files, InstallerMetadata.Uninstaller);

        var manifestPath = _manifestService.GetManifestPath(installLocation);

        // 构建安装清单
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
            // 卸载时默认保留用户配置和日志
            Preserve = ["user.config", "logs"]
        };

        // 写入清单文件和状态文件
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

    /// <summary>
    /// 将当前安装器可执行文件复制到安装目录作为卸载程序，
    /// 同时复制运行时所需的支持 DLL 文件。
    /// 如果已存在同路径则跳过复制。
    /// </summary>
    /// <param name="installLocation">安装目录</param>
    /// <param name="files">文件清单（会追加复制的新文件）</param>
    /// <exception cref="FileNotFoundException">无法定位当前安装器可执行文件时抛出</exception>
    private static void CopyUninstaller(string installLocation, List<string> files)
    {
        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
        {
            throw new FileNotFoundException("Unable to locate current installer executable.");
        }

        var uninstallerPath = Path.Combine(installLocation, InstallerMetadata.Uninstaller);
        if (Path.GetFullPath(currentExecutable).Equals(
                Path.GetFullPath(uninstallerPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Copy(currentExecutable, uninstallerPath, overwrite: true);

        // 复制卸载程序依赖的运行时 DLL
        var sourceDirectory = AppContext.BaseDirectory;
        foreach (var supportFile in UninstallerSupportFiles)
        {
            var sourcePath = Path.Combine(sourceDirectory, supportFile);
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var destinationPath = Path.Combine(installLocation, supportFile);
            if (!Path.GetFullPath(sourcePath).Equals(
                    Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }

            AddIfMissing(files, supportFile);
        }
    }

    /// <summary>
    /// 如果列表中不包含指定值则添加（不区分大小写）。
    /// </summary>
    /// <param name="items">目标列表</param>
    /// <param name="value">要添加的值</param>
    private static void AddIfMissing(List<string> items, string value)
    {
        if (!items.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(value);
        }
    }
}
