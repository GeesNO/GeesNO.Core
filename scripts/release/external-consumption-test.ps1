[CmdletBinding()]
param(
    [string]$ExpectedVersion = "0.1.0-preview.1",
    [string]$DryRunOutputRoot = "",
    [string]$ArtifactsRoot = "artifacts/external-consumption",
    [string[]]$AllowedUntrackedFile = @(),
    [switch]$AllowPendingChanges,
    [switch]$KeepWorkspace
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$RunId = "external-consumption-" + (Get-Date -Format "yyyyMMdd-HHmmss") + "-" + ([Guid]::NewGuid().ToString("N").Substring(0, 8))
if ([string]::IsNullOrWhiteSpace($DryRunOutputRoot)) {
    $DryRunOutputRoot = "artifacts/release-dry-run/$RunId-package-source"
}

if ([System.IO.Path]::IsPathRooted($DryRunOutputRoot)) {
    $DryRunOutputRootFullPath = [System.IO.Path]::GetFullPath($DryRunOutputRoot)
}
else {
    $DryRunOutputRootFullPath = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot $DryRunOutputRoot))
}

if ([System.IO.Path]::IsPathRooted($ArtifactsRoot)) {
    $ArtifactsRootFullPath = [System.IO.Path]::GetFullPath($ArtifactsRoot)
}
else {
    $ArtifactsRootFullPath = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot $ArtifactsRoot))
}

$SummaryDirectory = Join-Path $ArtifactsRootFullPath $RunId
$SummaryPath = Join-Path $SummaryDirectory "external-consumption-summary.json"
$WorkspaceRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("GeesNO.ExternalConsumption." + $RunId)
$FeedDirectory = Join-Path $WorkspaceRoot "feed"
$ConsumerDirectory = Join-Path $WorkspaceRoot "consumer"
$IsolatedPackagesDirectory = Join-Path $WorkspaceRoot "nuget-packages"
$IsolatedHttpCacheDirectory = Join-Path $WorkspaceRoot "nuget-http-cache"
$NuGetConfigPath = Join-Path $WorkspaceRoot "NuGet.Config"
$SampleSourceDirectory = Join-Path $RepositoryRoot "samples\GeesNO.Minimal.Integration"
$SampleProjectFileName = "GeesNO.Minimal.Integration.csproj"
$ConsumerProject = Join-Path $ConsumerDirectory $SampleProjectFileName
$AssetsPath = Join-Path $ConsumerDirectory "obj\project.assets.json"

$ExpectedPackageIds = @(
    "GeesNO.Core.Primitives",
    "GeesNO.Core.Contracts",
    "GeesNO.Core.Runtime"
)

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Invoke-Checked {
    param(
        [string]$FileName,
        [string[]]$Arguments,
        [string]$WorkingDirectory = $RepositoryRoot
    )

    Write-Host ("$FileName {0}" -f ($Arguments -join " "))
    Push-Location $WorkingDirectory
    try {
        & $FileName @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code ${LASTEXITCODE}: $FileName $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-Captured {
    param(
        [string]$FileName,
        [string[]]$Arguments,
        [string]$WorkingDirectory = $RepositoryRoot
    )

    Write-Host ("$FileName {0}" -f ($Arguments -join " "))
    Push-Location $WorkingDirectory
    try {
        $output = & $FileName @Arguments 2>&1
        $exitCode = $LASTEXITCODE
        $output | ForEach-Object { Write-Host $_ }
        if ($exitCode -ne 0) {
            throw "Command failed with exit code ${exitCode}: $FileName $($Arguments -join ' ')"
        }

        return @($output | ForEach-Object { $_.ToString() })
    }
    finally {
        Pop-Location
    }
}

function Assert-ChildPath {
    param(
        [string]$Parent,
        [string]$Child,
        [string]$Message
    )

    $parentComparable = ([System.IO.Path]::GetFullPath($Parent)).TrimEnd('\', '/') + '\'
    $childComparable = ([System.IO.Path]::GetFullPath($Child)).TrimEnd('\', '/') + '\'
    if ($childComparable -eq $parentComparable -or
        -not $childComparable.StartsWith($parentComparable, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw $Message
    }
}

function Assert-Equal {
    param(
        [object]$Expected,
        [object]$Actual,
        [string]$Message
    )

    if ($Expected -ne $Actual) {
        throw "$Message Expected '$Expected' but found '$Actual'."
    }
}

function Assert-ContainsLine {
    param(
        [string[]]$Lines,
        [string]$Pattern,
        [string]$Message
    )

    $matches = @($Lines | Where-Object { $_ -match $Pattern })
    if ($matches.Count -eq 0) {
        throw $Message
    }
}

function Assert-TextDoesNotContain {
    param(
        [string]$Text,
        [string[]]$Forbidden,
        [string]$Context
    )

    foreach ($item in $Forbidden) {
        if ($Text.IndexOf($item, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "$Context contains forbidden text '$item'."
        }
    }
}

function Copy-SampleSource {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Sample source '$Source' does not exist."
    }

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $sourceRoot = ([System.IO.Path]::GetFullPath($Source)).TrimEnd('\', '/') + '\'
    foreach ($item in Get-ChildItem -LiteralPath $Source -Recurse -Force) {
        $relativePath = $item.FullName.Substring($sourceRoot.Length)
        $segments = $relativePath -split '[\\/]'
        if ($segments -contains "bin" -or $segments -contains "obj") {
            continue
        }

        $target = Join-Path $Destination $relativePath
        if ($item.PSIsContainer) {
            New-Item -ItemType Directory -Path $target -Force | Out-Null
            continue
        }

        $targetDirectory = Split-Path -Parent $target
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        Copy-Item -LiteralPath $item.FullName -Destination $target
    }
}

function Test-IsolatedConsumerSource {
    param([string]$Path)

    $sourceFiles = @(Get-ChildItem -LiteralPath $Path -File -Recurse |
        Where-Object {
            $_.Extension -in @(".cs", ".csproj", ".props", ".targets") -or
            $_.Name -eq "NuGet.Config"
        })

    $forbiddenText = @(
        $RepositoryRoot,
        $RepositoryRoot.Replace("\", "/"),
        "<ProjectReference",
        "<Reference",
        "HintPath",
        "GeesNO.Reference.Simulator",
        "GeesNO.Core.Host",
        "GeesNO.Sample.Cli",
        "GeesNO.Core.Tests",
        "src\GeesNO",
        "src/GeesNO",
        '.cs" Link=',
        ".cs' Link="
    )

    foreach ($file in $sourceFiles) {
        $text = Get-Content -Raw -LiteralPath $file.FullName
        Assert-TextDoesNotContain -Text $text -Forbidden $forbiddenText -Context $file.FullName
    }

    $preBuildDlls = @(Get-ChildItem -LiteralPath $WorkspaceRoot -Filter "*.dll" -File -Recurse -ErrorAction SilentlyContinue)
    if ($preBuildDlls.Count -gt 0) {
        throw "External workspace contains DLL files before build: $($preBuildDlls.FullName -join '; ')"
    }
}

function Test-PackageAssets {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "project.assets.json was not generated."
    }

    $assetsText = Get-Content -Raw -LiteralPath $Path
    Assert-TextDoesNotContain -Text $assetsText -Forbidden @(
        $RepositoryRoot,
        $RepositoryRoot.Replace("\", "/"),
        "GeesNO.Reference.Simulator",
        "GeesNO.Core.Host",
        "GeesNO.Sample.Cli",
        "GeesNO.Core.Tests"
    ) -Context "project.assets.json"

    $assets = $assetsText | ConvertFrom-Json
    $libraries = $assets.libraries.PSObject.Properties

    foreach ($packageId in $ExpectedPackageIds) {
        $libraryName = "$packageId/$ExpectedVersion"
        $library = $libraries | Where-Object { $_.Name -eq $libraryName } | Select-Object -First 1
        if ($null -eq $library) {
            throw "project.assets.json does not contain package '$libraryName'."
        }

        Assert-Equal -Expected "package" -Actual $library.Value.type -Message "$libraryName must resolve as a NuGet package."
    }

    $projectReferences = @($libraries | Where-Object { $_.Value.type -eq "project" })
    if ($projectReferences.Count -gt 0) {
        throw "project.assets.json contains project references: $($projectReferences.Name -join '; ')"
    }

    $packageFolders = @($assets.packageFolders.PSObject.Properties.Name)
    if ($packageFolders.Count -ne 1) {
        throw "Expected exactly one isolated NuGet package folder but found: $($packageFolders -join '; ')"
    }

    $isolatedComparable = $IsolatedPackagesDirectory.TrimEnd('\', '/') + '\'
    $folderComparable = ([System.IO.Path]::GetFullPath($packageFolders[0])).TrimEnd('\', '/') + '\'
    Assert-Equal -Expected $isolatedComparable -Actual $folderComparable -Message "NuGet packages were not restored into the isolated workspace cache."

    $target = $assets.targets.PSObject.Properties | Select-Object -First 1
    if ($null -eq $target) {
        throw "project.assets.json does not contain restore targets."
    }

    $targetLibraries = $target.Value.PSObject.Properties
    $runtimeTargetLibrary = $targetLibraries |
        Where-Object { $_.Name -eq "GeesNO.Core.Runtime/$ExpectedVersion" } |
        Select-Object -First 1
    if ($null -eq $runtimeTargetLibrary) {
        throw "project.assets.json target does not contain GeesNO.Core.Runtime/$ExpectedVersion."
    }

    $runtimeDependencies = @($runtimeTargetLibrary.Value.dependencies.PSObject.Properties | ForEach-Object { "$($_.Name):$($_.Value)" } | Sort-Object)
    $expectedRuntimeDependencies = @(
        "GeesNO.Core.Contracts:$ExpectedVersion",
        "GeesNO.Core.Primitives:$ExpectedVersion"
    )

    $expectedJoined = ($expectedRuntimeDependencies | Sort-Object) -join "|"
    $actualJoined = ($runtimeDependencies | Sort-Object) -join "|"
    Assert-Equal -Expected $expectedJoined -Actual $actualJoined -Message "Runtime transitive dependencies mismatch."
}

function Get-PackageCommit {
    param([string]$PackagePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            throw "No nuspec entry found in '$PackagePath'."
        }

        $stream = $nuspecEntry.Open()
        try {
            $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
            try {
                $nuspecText = $reader.ReadToEnd().TrimStart([char]0xFEFF)
            }
            finally {
                $reader.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }

        $xml = New-Object System.Xml.XmlDocument
        $xml.LoadXml($nuspecText)
        $repository = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='repository']")
        if ($null -eq $repository -or $null -eq $repository.Attributes["commit"]) {
            return ""
        }

        return $repository.Attributes["commit"].Value
    }
    finally {
        $zip.Dispose()
    }
}

Set-Location $RepositoryRoot

Assert-ChildPath -Parent (Join-Path $RepositoryRoot "artifacts\release-dry-run") -Child $DryRunOutputRootFullPath -Message "Dry-run output must be inside artifacts\release-dry-run."
Assert-ChildPath -Parent (Join-Path $RepositoryRoot "artifacts") -Child $ArtifactsRootFullPath -Message "External consumption artifacts must be inside artifacts."
if ($WorkspaceRoot.StartsWith($RepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "External workspace must not be inside the GeesNO.Core repository."
}

$head = (& git rev-parse HEAD).Trim()
$dotnetVersion = (& dotnet --version).Trim()
$dotnetSdks = @(& dotnet --list-sdks)

Write-Step "Running official preview dry-run"
$previewScript = Join-Path $RepositoryRoot "scripts\release\preview-dry-run.ps1"
$previewArgs = @(
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    $previewScript,
    "-OutputRoot",
    $DryRunOutputRoot
)
if ($AllowPendingChanges) {
    $previewArgs += "-AllowPendingChanges"
}
foreach ($allowedFile in $AllowedUntrackedFile) {
    $previewArgs += "-AllowedUntrackedFile"
    $previewArgs += $allowedFile
}

Invoke-Checked "powershell" $previewArgs $RepositoryRoot

Write-Step "Creating external workspace"
New-Item -ItemType Directory -Path $FeedDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $IsolatedPackagesDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $IsolatedHttpCacheDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $SummaryDirectory -Force | Out-Null

$dryRunPackagesDirectory = Join-Path $DryRunOutputRootFullPath "packages"
$copiedPackages = @()
foreach ($packageId in $ExpectedPackageIds) {
    $packageName = "$packageId.$ExpectedVersion.nupkg"
    $source = Join-Path $dryRunPackagesDirectory $packageName
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Expected package '$source' was not produced by preview dry-run."
    }

    Copy-Item -LiteralPath $source -Destination $FeedDirectory
    $copiedPackages += $packageName
}

$feedPackages = @(Get-ChildItem -LiteralPath $FeedDirectory -File | Select-Object -ExpandProperty Name | Sort-Object)
$expectedFeed = @($ExpectedPackageIds | ForEach-Object { "$_.$ExpectedVersion.nupkg" } | Sort-Object)
Assert-Equal -Expected (($expectedFeed | Sort-Object) -join "|") -Actual (($feedPackages | Sort-Object) -join "|") -Message "External feed package list mismatch."

$escapedFeedDirectory = [System.Security.SecurityElement]::Escape($FeedDirectory)
$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="geesno-local-preview" value="$escapedFeedDirectory" />
  </packageSources>
</configuration>
"@
$nugetConfig | Set-Content -LiteralPath $NuGetConfigPath -Encoding UTF8

Write-Step "Copying minimal external integration sample"
Copy-SampleSource -Source $SampleSourceDirectory -Destination $ConsumerDirectory
Test-IsolatedConsumerSource -Path $WorkspaceRoot

Write-Step "Restoring, building, and running external consumer"
$oldNuGetPackages = $env:NUGET_PACKAGES
$oldNuGetHttpCache = $env:NUGET_HTTP_CACHE_PATH
try {
    $env:NUGET_PACKAGES = $IsolatedPackagesDirectory
    $env:NUGET_HTTP_CACHE_PATH = $IsolatedHttpCacheDirectory

    $restoreOutput = Invoke-Captured "dotnet" @("restore", $ConsumerProject, "--configfile", $NuGetConfigPath, "--packages", $IsolatedPackagesDirectory, "--no-cache") $WorkspaceRoot
    $packageGraphOutput = Invoke-Captured "dotnet" @("list", $ConsumerProject, "package", "--include-transitive") $WorkspaceRoot
    $buildOutput = Invoke-Captured "dotnet" @("build", $ConsumerProject, "-c", "Release", "--no-restore") $WorkspaceRoot
    $runOutput = Invoke-Captured "dotnet" @("run", "--project", $ConsumerProject, "-c", "Release", "--no-build") $WorkspaceRoot
}
finally {
    $env:NUGET_PACKAGES = $oldNuGetPackages
    $env:NUGET_HTTP_CACHE_PATH = $oldNuGetHttpCache
}

Assert-ContainsLine -Lines $packageGraphOutput -Pattern "GeesNO\.Core\.Runtime\s+0\.1\.0-preview\.1" -Message "Runtime package was not observed in the package graph."
Assert-ContainsLine -Lines $packageGraphOutput -Pattern "GeesNO\.Core\.Contracts\s+0\.1\.0-preview\.1" -Message "Contracts package was not observed transitively in the package graph."
Assert-ContainsLine -Lines $packageGraphOutput -Pattern "GeesNO\.Core\.Primitives\s+0\.1\.0-preview\.1" -Message "Primitives package was not observed transitively in the package graph."
Assert-ContainsLine -Lines $runOutput -Pattern "^positive=passed status=Success" -Message "Positive external execution did not pass."
Assert-ContainsLine -Lines $runOutput -Pattern "^fail_closed=passed status=Rejected" -Message "Fail-closed external execution did not pass."

Test-PackageAssets -Path $AssetsPath

[xml]$consumerProjectXml = Get-Content -Raw -LiteralPath $ConsumerProject
$packageReferences = @($consumerProjectXml.Project.ItemGroup.PackageReference)
if ($packageReferences.Count -ne 1) {
    throw "External consumer must have exactly one direct PackageReference."
}

Assert-Equal -Expected "GeesNO.Core.Runtime" -Actual $packageReferences[0].Include -Message "External consumer direct package mismatch."
Assert-Equal -Expected $ExpectedVersion -Actual $packageReferences[0].Version -Message "External consumer direct package version mismatch."

$packageCommits = @()
foreach ($packageId in $ExpectedPackageIds) {
    $packagePath = Join-Path $FeedDirectory "$packageId.$ExpectedVersion.nupkg"
    $packageCommit = Get-PackageCommit -PackagePath $packagePath
    Assert-Equal -Expected $head -Actual $packageCommit -Message "$packageId package commit metadata mismatch."
    $packageCommits += [pscustomobject]@{
        PackageId = $packageId
        RepositoryCommit = $packageCommit
    }
}

$summary = [pscustomobject]@{
    RunId = $RunId
    Version = $ExpectedVersion
    RepositoryHead = $head
    DotNetVersion = $dotnetVersion
    DotNetSdks = $dotnetSdks
    WorkspaceRoot = $WorkspaceRoot
    FeedDirectory = $FeedDirectory
    NuGetConfigPath = $NuGetConfigPath
    IsolatedPackagesDirectory = $IsolatedPackagesDirectory
    SampleSourceDirectory = $SampleSourceDirectory
    DryRunOutputRoot = $DryRunOutputRootFullPath
    DirectPackage = "GeesNO.Core.Runtime:$ExpectedVersion"
    FeedPackages = $feedPackages
    PackageCommits = $packageCommits
    RestoreOutput = $restoreOutput
    PackageGraphOutput = $packageGraphOutput
    BuildOutput = $buildOutput
    RunOutput = $runOutput
    PositiveTest = "passed"
    FailClosedTest = "passed"
    IsolationChecks = "passed"
}

$summary |
    ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $SummaryPath -Encoding UTF8

Write-Step "External consumption test completed"
Write-Host "Workspace: $WorkspaceRoot"
Write-Host "Feed: $FeedDirectory"
Write-Host "NuGet.Config: $NuGetConfigPath"
Write-Host "Summary: $SummaryPath"

if (-not $KeepWorkspace) {
    Remove-Item -LiteralPath $WorkspaceRoot -Recurse -Force
    Write-Host "Workspace removed. Use -KeepWorkspace to retain it for inspection."
}
