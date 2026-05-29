using System.Text.Json.Serialization;

namespace AvaInstaller.Models.Json;

/// <summary>
/// Native AOT 友好的 System.Text.Json 源生成上下文。
/// 避免运行时反射序列化，确保在 Native AOT 编译环境下正常工作。
/// 
/// 配置说明：
/// - WriteIndented = true: JSON 输出格式化缩进
/// - CamelCase 命名策略: 属性名使用驼峰式（如 "appName"）
/// - InstallManifest 和 InstallState 类型在此注册序列化
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(InstallManifest))]
[JsonSerializable(typeof(InstallState))]
internal partial class InstallerJsonContext : JsonSerializerContext;
