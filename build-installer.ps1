param(
    [string]$PayloadPath = ".\payload",
    [string]$ProjectPath = ".\Installer\Installer.csproj",
    [string]$Configuration = "Release",
    [ValidateSet("Aot", "SingleFile")]
    [string]$PublishMode = "Aot"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$payloadFullPath = [System.IO.Path]::GetFullPath((Join-Path $root $PayloadPath))
$projectFullPath = [System.IO.Path]::GetFullPath((Join-Path $root $ProjectPath))
$resourcesDirectory = Join-Path (Split-Path -Parent $projectFullPath) "Resources"
$payloadZipPath = Join-Path $resourcesDirectory "payload.zip"
$distDirectory = Join-Path $root "dist"

if (-not (Test-Path -LiteralPath $payloadFullPath -PathType Container)) {
    throw "Payload directory was not found: $payloadFullPath"
}

New-Item -ItemType Directory -Force -Path $resourcesDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $distDirectory | Out-Null

if (Test-Path -LiteralPath $payloadZipPath) {
    Remove-Item -LiteralPath $payloadZipPath -Force
}

$payloadItems = Get-ChildItem -LiteralPath $payloadFullPath -Force
if ($payloadItems.Count -eq 0) {
    throw "Payload directory is empty: $payloadFullPath"
}

Compress-Archive -Path (Join-Path $payloadFullPath "*") -DestinationPath $payloadZipPath -CompressionLevel Optimal -Force

$publishProperties = @(
    "/p:PublishDir=$distDirectory\",
    "/p:AssemblyName=MyAvaloniaAppInstaller",
    "/p:PublishSingleFile=true",
    "/p:SelfContained=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true"
)

if ($PublishMode -eq "Aot") {
    $publishProperties += "/p:PublishAot=true"
} else {
    $publishProperties += "/p:PublishAot=false"
}

dotnet publish $projectFullPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    @publishProperties

$installerPath = Join-Path $distDirectory "MyAvaloniaAppInstaller.exe"
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Publish finished, but installer exe was not found: $installerPath"
}

Write-Host "Created installer: $installerPath"
