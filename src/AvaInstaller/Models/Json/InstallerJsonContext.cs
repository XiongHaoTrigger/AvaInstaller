using System.Text.Json.Serialization;

namespace AvaInstaller.Models.Json;

// Native AOT 友好的 System.Text.Json source generation，避免运行时反射序列化。
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(InstallManifest))]
[JsonSerializable(typeof(InstallState))]
internal partial class InstallerJsonContext : JsonSerializerContext;
