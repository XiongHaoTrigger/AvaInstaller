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
    Set-ItemProperty -LiteralPath $payloadZipPath -Name IsReadOnly -Value $false -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $payloadZipPath -Force
}

$payloadItems = Get-ChildItem -LiteralPath $payloadFullPath -Force
if ($payloadItems.Count -eq 0) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $emptyZip = [System.IO.Compression.ZipFile]::Open($payloadZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    $emptyZip.Dispose()
    Write-Host "Payload directory is empty; created an empty payload archive for installer smoke testing."
} else {
    Compress-Archive -Path (Join-Path $payloadFullPath "*") -DestinationPath $payloadZipPath -CompressionLevel Optimal -Force
}

$publishProperties = @(
    "/p:PublishDir=$distDirectory\",
    "/p:AssemblyName=MyAvaloniaAppInstaller",
    "/p:SelfContained=true"
)

if ($PublishMode -eq "Aot") {
    Write-Host "Publishing .NET 10 Native AOT installer. Native AOT emits a native exe; PublishSingleFile is intentionally disabled."
    $publishProperties += "/p:PublishAot=true"
    $publishProperties += "/p:PublishSingleFile=false"
    $publishProperties += "/p:IlcUseEnvironmentalTools=true"
} else {
    Write-Host "Publishing .NET 10 self-contained single-file installer."
    $publishProperties += "/p:PublishAot=false"
    $publishProperties += "/p:PublishSingleFile=true"
    $publishProperties += "/p:IncludeNativeLibrariesForSelfExtract=true"
    $publishProperties += "/p:EnableCompressionInSingleFile=true"
}

function Get-VcVars64Path {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
        return $null
    }

    $json = & $vswhere -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($json)) {
        return $null
    }

    $instances = $json | ConvertFrom-Json
    if ($null -eq $instances -or $instances.Count -eq 0) {
        return $null
    }

    $installationPath = @($instances)[0].installationPath
    if ([string]::IsNullOrWhiteSpace($installationPath)) {
        return $null
    }

    $candidate = Join-Path $installationPath "VC\Auxiliary\Build\vcvars64.bat"
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        return $candidate
    }

    return $null
}

function Quote-CmdArgument([string]$Value) {
    '"' + ($Value -replace '"', '\"') + '"'
}

$publishFailed = $false
if ($PublishMode -eq "Aot") {
    $vcvars64Path = Get-VcVars64Path
    if ([string]::IsNullOrWhiteSpace($vcvars64Path)) {
        throw "Native AOT publish failed before build: Visual Studio vcvars64.bat was not found."
    }

    $publishArgs = @(
        "publish",
        (Quote-CmdArgument $projectFullPath),
        "-c",
        (Quote-CmdArgument $Configuration),
        "-r",
        "win-x64",
        "--self-contained",
        "true"
    ) + $publishProperties

    $publishCommand = "call $(Quote-CmdArgument $vcvars64Path) && dotnet $($publishArgs -join ' ')"
    cmd.exe /d /s /c $publishCommand
} else {
    dotnet publish $projectFullPath `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        @publishProperties
}

if ($LASTEXITCODE -ne 0) {
    $publishFailed = $true
}

if ($publishFailed) {
    if ($PublishMode -eq "Aot") {
        throw "Native AOT publish failed. On Windows, install Visual Studio Desktop development with C++ workload, including MSVC x64/x86 build tools and Windows SDK, then rerun this script."
    }

    throw "Publish failed."
}

$installerPath = Join-Path $distDirectory "MyAvaloniaAppInstaller.exe"
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Publish finished, but installer exe was not found: $installerPath"
}

Write-Host "Created installer: $installerPath"
