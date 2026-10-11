[CmdletBinding()]
param(
    [string]$ExpectedBranch = "",
    [string]$ExpectedVersion = "0.1.0-preview.1",
    [string]$OutputRoot = "artifacts/release-dry-run/0.1.0-preview.1",
    [string[]]$AllowedUntrackedFile = @(),
    [switch]$AllowPendingChanges
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRootFullPath = [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    $OutputRootFullPath = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot $OutputRoot))
}

$ReleaseDryRunRootFullPath = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot "artifacts\release-dry-run"))
$PackagesDirectory = Join-Path $OutputRootFullPath "packages"

$ExpectedAuthor = "GENARO ESCUDERO NOGALES"
$ExpectedLicense = "Apache-2.0"
$ExpectedFramework = "net8.0"
$ExpectedRepositoryUrl = "https://github.com/GeesNO/GeesNO.Core"

$PublicPackages = @(
    [pscustomobject]@{
        Id = "GeesNO.Core.Primitives"
        Project = "src\GeesNO.Core.Primitives\GeesNO.Core.Primitives.csproj"
        Description = "Stable, domain-neutral primitives shared by GeesNO components."
        Dependencies = @()
    },
    [pscustomobject]@{
        Id = "GeesNO.Core.Contracts"
        Project = "src\GeesNO.Core.Contracts\GeesNO.Core.Contracts.csproj"
        Description = "Public domain-neutral contracts for GeesNO capabilities, operations and tools."
        Dependencies = @("GeesNO.Core.Primitives:$ExpectedVersion")
    },
    [pscustomobject]@{
        Id = "GeesNO.Core.Runtime"
        Project = "src\GeesNO.Core.Runtime\GeesNO.Core.Runtime.csproj"
        Description = "Public headless Runtime facade for executing typed GeesNO.Core operations."
        Dependencies = @("GeesNO.Core.Contracts:$ExpectedVersion", "GeesNO.Core.Primitives:$ExpectedVersion")
    }
)

$NonPackableProjects = @(
    "src\GeesNO.Reference.Simulator\GeesNO.Reference.Simulator.csproj",
    "src\GeesNO.Core.Host\GeesNO.Core.Host.csproj",
    "samples\GeesNO.Sample.Cli\GeesNO.Sample.Cli.csproj",
    "tests\GeesNO.Core.Tests\GeesNO.Core.Tests.csproj"
)

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Invoke-Checked {
    param(
        [string]$FileName,
        [string[]]$Arguments
    )

    Write-Host ("$FileName {0}" -f ($Arguments -join " "))
    & $FileName @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FileName $($Arguments -join ' ')"
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

function Assert-SequenceEqual {
    param(
        [string[]]$Expected,
        [string[]]$Actual,
        [string]$Message
    )

    $expectedJoined = ($Expected | Sort-Object) -join "|"
    $actualJoined = ($Actual | Sort-Object) -join "|"
    if ($expectedJoined -ne $actualJoined) {
        throw "$Message Expected '$expectedJoined' but found '$actualJoined'."
    }
}

function Get-XmlPropertyValue {
    param(
        [xml]$Document,
        [string]$Name
    )

    $node = $Document.Project.PropertyGroup |
        ForEach-Object { $_.ChildNodes } |
        Where-Object { $_.LocalName -eq $Name } |
        Select-Object -First 1

    if ($null -eq $node) {
        return $null
    }

    return $node.InnerText
}

function Get-ChildText {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$Name
    )

    $child = $Node.ChildNodes |
        Where-Object { $_.LocalName -eq $Name } |
        Select-Object -First 1

    if ($null -eq $child) {
        return ""
    }

    return $child.InnerText
}

function Get-XmlAttribute {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$Name
    )

    if ($null -eq $Node -or $null -eq $Node.Attributes[$Name]) {
        return ""
    }

    return $Node.Attributes[$Name].Value
}

function Read-ZipEntryBytes {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryName
    )

    $entry = $Zip.Entries | Where-Object { $_.FullName -eq $EntryName } | Select-Object -First 1
    if ($null -eq $entry) {
        throw "Package entry '$EntryName' was not found."
    }

    $stream = $entry.Open()
    try {
        $memory = New-Object System.IO.MemoryStream
        try {
            $stream.CopyTo($memory)
            return $memory.ToArray()
        }
        finally {
            $memory.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Read-ZipEntryText {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryName
    )

    $bytes = Read-ZipEntryBytes -Zip $Zip -EntryName $EntryName
    return [System.Text.Encoding]::UTF8.GetString($bytes)
}

function Normalize-PackageEntry {
    param([string]$EntryName)

    if ($EntryName -like "package/services/metadata/core-properties/*.psmdcp") {
        return "package/services/metadata/core-properties/<core-properties>.psmdcp"
    }

    return $EntryName
}

function Assert-ZipDoesNotContainForbiddenText {
    param(
        [string]$PackagePath,
        [string[]]$ForbiddenPatterns
    )

    $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        foreach ($entry in $zip.Entries) {
            if ($entry.Length -eq 0) {
                continue
            }

            $bytes = Read-ZipEntryBytes -Zip $zip -EntryName $entry.FullName
            $utf8 = [System.Text.Encoding]::UTF8.GetString($bytes)
            $unicode = [System.Text.Encoding]::Unicode.GetString($bytes)

            foreach ($pattern in $ForbiddenPatterns) {
                if ($utf8.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                    $unicode.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    throw "Forbidden text '$pattern' found in '$PackagePath' entry '$($entry.FullName)'."
                }
            }
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Get-NuspecInspection {
    param(
        [string]$PackagePath
    )

    $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            throw "No nuspec entry found in '$PackagePath'."
        }

        $nuspecText = (Read-ZipEntryText -Zip $zip -EntryName $nuspecEntry.FullName).TrimStart([char]0xFEFF)
        $nuspec = New-Object System.Xml.XmlDocument
        $nuspec.PreserveWhitespace = $false
        $nuspec.LoadXml($nuspecText)
        $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
        if ($null -eq $metadata) {
            throw "No metadata node found in '$($nuspecEntry.FullName)' from '$PackagePath'."
        }
        $license = $metadata.ChildNodes | Where-Object { $_.LocalName -eq "license" } | Select-Object -First 1
        $repository = $metadata.ChildNodes | Where-Object { $_.LocalName -eq "repository" } | Select-Object -First 1
        $dependencies = @()

        foreach ($dependency in $metadata.SelectNodes(".//*[local-name()='dependency']")) {
            $dependencies += [pscustomobject]@{
                Id = Get-XmlAttribute -Node $dependency -Name "id"
                Version = Get-XmlAttribute -Node $dependency -Name "version"
            }
        }

        return [pscustomobject]@{
            NuspecEntry = $nuspecEntry.FullName
            Id = Get-ChildText -Node $metadata -Name "id"
            Version = Get-ChildText -Node $metadata -Name "version"
            Authors = Get-ChildText -Node $metadata -Name "authors"
            Description = Get-ChildText -Node $metadata -Name "description"
            Readme = Get-ChildText -Node $metadata -Name "readme"
            License = if ($null -eq $license) { "" } else { $license.InnerText }
            LicenseType = Get-XmlAttribute -Node $license -Name "type"
            RepositoryType = Get-XmlAttribute -Node $repository -Name "type"
            RepositoryUrl = Get-XmlAttribute -Node $repository -Name "url"
            RepositoryCommit = Get-XmlAttribute -Node $repository -Name "commit"
            ProjectUrl = Get-ChildText -Node $metadata -Name "projectUrl"
            Dependencies = $dependencies
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Test-MarkdownLinks {
    param([string[]]$Files)

    $errors = @()
    foreach ($file in $Files) {
        $path = Join-Path $RepositoryRoot $file
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Markdown link validation source '$file' does not exist."
        }

        $text = Get-Content -Raw -LiteralPath $path
        $directory = Split-Path -Parent $path
        $matches = [regex]::Matches($text, "\[[^\]]+\]\(([^)]+)\)")
        foreach ($match in $matches) {
            $target = $match.Groups[1].Value
            if ($target -match "^(https?://|mailto:|#)") {
                continue
            }

            $targetNoAnchor = $target.Split("#")[0]
            if ([string]::IsNullOrWhiteSpace($targetNoAnchor)) {
                continue
            }

            $candidate = Join-Path $directory $targetNoAnchor
            if (-not (Test-Path -LiteralPath $candidate)) {
                $errors += "$file -> $target"
            }
        }
    }

    if ($errors.Count -gt 0) {
        throw "Broken relative Markdown links detected: $($errors -join '; ')"
    }
}

function Test-Package {
    param(
        [pscustomobject]$ExpectedPackage,
        [string]$PackagePath,
        [string]$SymbolPackagePath,
        [string]$Head
    )

    $id = $ExpectedPackage.Id
    $dllEntry = "lib/$ExpectedFramework/$id.dll"
    $xmlEntry = "lib/$ExpectedFramework/$id.xml"
    $pdbEntry = "lib/$ExpectedFramework/$id.pdb"
    $expectedNupkgEntries = @(
        "_rels/.rels",
        "[Content_Types].xml",
        "$id.nuspec",
        $dllEntry,
        $xmlEntry,
        "PACKAGE_README.md",
        "package/services/metadata/core-properties/<core-properties>.psmdcp"
    )
    $expectedSnupkgEntries = @(
        "_rels/.rels",
        "[Content_Types].xml",
        "$id.nuspec",
        $pdbEntry,
        "package/services/metadata/core-properties/<core-properties>.psmdcp"
    )

    $inspection = Get-NuspecInspection -PackagePath $PackagePath

    Assert-Equal -Expected $id -Actual $inspection.Id -Message "PackageId mismatch for $id."
    Assert-Equal -Expected $ExpectedVersion -Actual $inspection.Version -Message "PackageVersion mismatch for $id."
    Assert-Equal -Expected $ExpectedAuthor -Actual $inspection.Authors -Message "Authors mismatch for $id."
    Assert-Equal -Expected $ExpectedLicense -Actual $inspection.License -Message "License mismatch for $id."
    Assert-Equal -Expected "expression" -Actual $inspection.LicenseType -Message "License type mismatch for $id."
    Assert-Equal -Expected $ExpectedPackage.Description -Actual $inspection.Description -Message "Description mismatch for $id."
    Assert-Equal -Expected "PACKAGE_README.md" -Actual $inspection.Readme -Message "Package README mismatch for $id."
    Assert-Equal -Expected "git" -Actual $inspection.RepositoryType -Message "Repository type mismatch for $id."
    Assert-Equal -Expected $ExpectedRepositoryUrl -Actual $inspection.RepositoryUrl -Message "RepositoryUrl mismatch for $id."
    Assert-Equal -Expected $ExpectedRepositoryUrl -Actual $inspection.ProjectUrl -Message "PackageProjectUrl mismatch for $id."
    Assert-Equal -Expected $Head -Actual $inspection.RepositoryCommit -Message "Repository commit mismatch for $id."

    $dependencySummary = @($inspection.Dependencies | ForEach-Object { "$($_.Id):$($_.Version)" })
    Assert-SequenceEqual -Expected ([string[]]$ExpectedPackage.Dependencies) -Actual ([string[]]$dependencySummary) -Message "Dependencies mismatch for $id."

    $nupkg = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $normalizedEntries = @($nupkg.Entries | ForEach-Object { Normalize-PackageEntry $_.FullName } | Sort-Object -Unique)
        Assert-SequenceEqual -Expected $expectedNupkgEntries -Actual ([string[]]$normalizedEntries) -Message "Nupkg entries mismatch for $id."

        $dllEntries = @($nupkg.Entries | Where-Object { $_.FullName -like "lib/*.dll" } | ForEach-Object { $_.FullName })
        Assert-SequenceEqual -Expected @($dllEntry) -Actual ([string[]]$dllEntries) -Message "DLL entries mismatch for $id."
    }
    finally {
        $nupkg.Dispose()
    }

    $snupkg = [System.IO.Compression.ZipFile]::OpenRead($SymbolPackagePath)
    try {
        $symbolEntries = @($snupkg.Entries | ForEach-Object { Normalize-PackageEntry $_.FullName } | Sort-Object -Unique)
        Assert-SequenceEqual -Expected $expectedSnupkgEntries -Actual ([string[]]$symbolEntries) -Message "Snupkg entries mismatch for $id."
    }
    finally {
        $snupkg.Dispose()
    }

    return [pscustomobject]@{
        Id = $inspection.Id
        Version = $inspection.Version
        Authors = $inspection.Authors
        Description = $inspection.Description
        License = $inspection.License
        LicenseType = $inspection.LicenseType
        Readme = $inspection.Readme
        RepositoryType = $inspection.RepositoryType
        RepositoryUrl = $inspection.RepositoryUrl
        RepositoryCommit = $inspection.RepositoryCommit
        ProjectUrl = $inspection.ProjectUrl
        TargetFramework = $ExpectedFramework
        Dependencies = @($dependencySummary | Sort-Object)
        NupkgEntries = $expectedNupkgEntries
        SnupkgEntries = $expectedSnupkgEntries
    }
}

Set-Location $RepositoryRoot

$releaseDryRunRootComparable = $ReleaseDryRunRootFullPath.TrimEnd('\', '/') + '\'
$outputRootComparable = $OutputRootFullPath.TrimEnd('\', '/') + '\'
if ($outputRootComparable -eq $releaseDryRunRootComparable -or
    -not $outputRootComparable.StartsWith($releaseDryRunRootComparable, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Output root must be a child of artifacts\release-dry-run: $OutputRootFullPath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Step "Collecting git and SDK state"
$branch = [string](& git branch --show-current)
$branch = $branch.Trim()
$head = (& git rev-parse HEAD).Trim()
$remote = (& git remote -v)
$dotnetVersion = (& dotnet --version).Trim()
$dotnetSdks = @(& dotnet --list-sdks)

if (-not [string]::IsNullOrWhiteSpace($ExpectedBranch)) {
    Assert-Equal -Expected $ExpectedBranch -Actual $branch -Message "Unexpected branch."
}

$statusLines = @(& git status --porcelain --untracked-files=all)
if (-not $AllowPendingChanges) {
    $unexpectedStatus = @()
    foreach ($line in $statusLines) {
        $allowed = $false
        foreach ($allowedFile in $AllowedUntrackedFile) {
            if ($line -eq "?? $allowedFile") {
                $allowed = $true
                break
            }
        }

        if ($allowed) {
            continue
        }

        $unexpectedStatus += $line
    }

    if ($unexpectedStatus.Count -gt 0) {
        throw "Working tree must be clean except explicitly allowed untracked files. Unexpected status: $($unexpectedStatus -join '; ')"
    }
}
else {
    Write-Host "AllowPendingChanges is set; pending working tree changes are allowed for this dry-run."
}

if (-not (Test-Path -LiteralPath ".github\pull_request_template.md")) {
    throw ".github/pull_request_template.md is missing from the repository."
}

if (Test-Path -LiteralPath "..\GeesNO.Core.github") {
    throw "Unexpected sibling directory '..\GeesNO.Core.github' exists."
}

Write-Host "Branch: $branch"
Write-Host "HEAD: $head"
Write-Host "dotnet --version: $dotnetVersion"
Write-Host "dotnet --list-sdks:"
$dotnetSdks | ForEach-Object { Write-Host "  $_" }
if ($remote) {
    Write-Host "Remote configuration present:"
    $remote | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "No git remote configured."
}

Write-Step "Checking package metadata preconditions"
[xml]$directoryBuildProps = Get-Content -Raw -LiteralPath "Directory.Build.props"
Assert-Equal -Expected $ExpectedVersion -Actual (Get-XmlPropertyValue -Document $directoryBuildProps -Name "GeesNODeveloperPreviewPackageVersion") -Message "Developer Preview package version mismatch."

foreach ($package in $PublicPackages) {
    [xml]$project = Get-Content -Raw -LiteralPath $package.Project
    Assert-Equal -Expected "true" -Actual (Get-XmlPropertyValue -Document $project -Name "IsPackable") -Message "$($package.Id) must be packable."
    Assert-Equal -Expected $package.Id -Actual (Get-XmlPropertyValue -Document $project -Name "PackageId") -Message "$($package.Id) PackageId mismatch."
    Assert-Equal -Expected '$(GeesNODeveloperPreviewPackageVersion)' -Actual (Get-XmlPropertyValue -Document $project -Name "PackageVersion") -Message "$($package.Id) PackageVersion must use the shared preview property."
    Assert-Equal -Expected "PACKAGE_README.md" -Actual (Get-XmlPropertyValue -Document $project -Name "PackageReadmeFile") -Message "$($package.Id) must include PACKAGE_README.md."
}

foreach ($projectPath in $NonPackableProjects) {
    [xml]$project = Get-Content -Raw -LiteralPath $projectPath
    Assert-Equal -Expected "false" -Actual (Get-XmlPropertyValue -Document $project -Name "IsPackable") -Message "$projectPath must remain non-packable."
    $packageId = Get-XmlPropertyValue -Document $project -Name "PackageId"
    if ($packageId) {
        throw "$projectPath must not declare PackageId."
    }
}

$trackedArtifacts = @(& git ls-files | Where-Object { $_ -match '(^artifacts/|\.nupkg$|\.snupkg$|SHA256SUMS\.txt$)' })
if ($trackedArtifacts.Count -gt 0) {
    throw "Package artifacts must not be tracked: $($trackedArtifacts -join '; ')"
}

Write-Step "Preparing clean dry-run output"
if (Test-Path -LiteralPath $OutputRootFullPath) {
    Remove-Item -LiteralPath $OutputRootFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $PackagesDirectory -Force | Out-Null

Write-Step "Running repository validation"
Invoke-Checked "dotnet" @("restore", "GeesNO.Core.sln")
Invoke-Checked "dotnet" @("build", "GeesNO.Core.sln", "-c", "Release", "--no-restore")
Invoke-Checked "dotnet" @("test", "GeesNO.Core.sln", "-c", "Release", "--no-build")
Invoke-Checked "dotnet" @("run", "--project", "src\GeesNO.Core.Host", "-c", "Release", "--no-build")
Invoke-Checked "dotnet" @("run", "--project", "samples\GeesNO.Sample.Cli", "-c", "Release", "--no-build", "--", "echo", "release-dry-run-smoke")
Invoke-Checked "dotnet" @("format", "GeesNO.Core.sln", "--verify-no-changes", "--no-restore")
Invoke-Checked "git" @("diff", "--check")
Invoke-Checked "dotnet" @("sln", "GeesNO.Core.sln", "list")

$trackedBinObj = @(& git ls-files | Where-Object { $_ -match '(^|/)(bin|obj)/' })
if ($trackedBinObj.Count -gt 0) {
    throw "Tracked bin/obj entries detected: $($trackedBinObj -join '; ')"
}

$expectedSourceProjects = @(
    "src/GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj",
    "src/GeesNO.Core.Host/GeesNO.Core.Host.csproj",
    "src/GeesNO.Core.Primitives/GeesNO.Core.Primitives.csproj",
    "src/GeesNO.Core.Runtime/GeesNO.Core.Runtime.csproj",
    "src/GeesNO.Reference.Simulator/GeesNO.Reference.Simulator.csproj"
)
$actualSourceProjects = @(& git ls-files "src/*.csproj" "src/*/*.csproj" |
    ForEach-Object { $_.Replace("\", "/") } |
    Sort-Object)
Assert-SequenceEqual -Expected $expectedSourceProjects -Actual ([string[]]$actualSourceProjects) -Message "Tracked source project list mismatch."

$trackedUnexpectedSourceProjects = @($actualSourceProjects |
    Where-Object { $expectedSourceProjects -notcontains $_ })
if ($trackedUnexpectedSourceProjects.Count -gt 0) {
    throw "Unexpected tracked source projects detected: $($trackedUnexpectedSourceProjects -join '; ')"
}

Test-MarkdownLinks @(
    "README.md",
    "docs\getting-started\quick-start.md",
    "docs\integration\README.md",
    "docs\integration\public-api.md",
    "CONTRIBUTING.md",
    "SUPPORT.md",
    "SECURITY.md",
    "TRADEMARKS.md",
    "RELEASE.md",
    "PROJECT_STATUS.md",
    "CHANGELOG.md",
    "docs\RepositoryIndex.md",
    "docs\releases\0.1.0-preview.1.md"
)

Write-Step "Packing public Developer Preview packages"
foreach ($package in $PublicPackages) {
    Invoke-Checked "dotnet" @("pack", $package.Project, "-c", "Release", "--no-build", "-o", $PackagesDirectory)
}

$expectedArtifactNames = @()
foreach ($package in $PublicPackages) {
    $expectedArtifactNames += "$($package.Id).$ExpectedVersion.nupkg"
    $expectedArtifactNames += "$($package.Id).$ExpectedVersion.snupkg"
}

$actualArtifactNames = @(Get-ChildItem -LiteralPath $PackagesDirectory -File |
    Where-Object { $_.Extension -in @(".nupkg", ".snupkg") } |
    Select-Object -ExpandProperty Name |
    Sort-Object)

Assert-SequenceEqual -Expected ([string[]]$expectedArtifactNames) -Actual ([string[]]$actualArtifactNames) -Message "Dry-run artifact list mismatch."

Write-Step "Inspecting packages"
$forbiddenText = @(
    $RepositoryRoot,
    $RepositoryRoot.Replace("\", "/"),
    "E:\",
    "C:\Users",
    "AppData",
    "\Temp\",
    "/Temp/",
    "PRIVATE KEY",
    "BEGIN RSA",
    "BEGIN OPENSSH",
    "api_key",
    "password=",
    "secret="
)

$inspectionResults = @()
foreach ($package in $PublicPackages) {
    $nupkgPath = Join-Path $PackagesDirectory "$($package.Id).$ExpectedVersion.nupkg"
    $snupkgPath = Join-Path $PackagesDirectory "$($package.Id).$ExpectedVersion.snupkg"

    Assert-ZipDoesNotContainForbiddenText -PackagePath $nupkgPath -ForbiddenPatterns $forbiddenText
    Assert-ZipDoesNotContainForbiddenText -PackagePath $snupkgPath -ForbiddenPatterns $forbiddenText

    $inspectionResults += Test-Package -ExpectedPackage $package -PackagePath $nupkgPath -SymbolPackagePath $snupkgPath -Head $head
}

Write-Step "Generating SHA-256 manifest"
$checksumRows = @()
foreach ($artifact in Get-ChildItem -LiteralPath $PackagesDirectory -File | Sort-Object Name) {
    $hash = Get-FileHash -LiteralPath $artifact.FullName -Algorithm SHA256
    $checksumRows += [pscustomobject]@{
        FileName = $artifact.Name
        SHA256 = $hash.Hash.ToLowerInvariant()
    }
}

$checksumPath = Join-Path $OutputRootFullPath "SHA256SUMS.txt"
$checksumRows |
    ForEach-Object { "$($_.SHA256)  packages/$($_.FileName)" } |
    Set-Content -LiteralPath $checksumPath -Encoding ASCII

$inspectionPath = Join-Path $OutputRootFullPath "package-inspection.json"
$inspectionResults |
    ConvertTo-Json -Depth 12 |
    Set-Content -LiteralPath $inspectionPath -Encoding UTF8

$summary = [pscustomobject]@{
    Version = $ExpectedVersion
    Branch = $branch
    Head = $head
    DotNetVersion = $dotnetVersion
    DotNetSdks = $dotnetSdks
    OutputRoot = $OutputRootFullPath
    PackagesDirectory = $PackagesDirectory
    Artifacts = $checksumRows
    Packages = $inspectionResults
}

$summaryPath = Join-Path $OutputRootFullPath "dry-run-summary.json"
$summary |
    ConvertTo-Json -Depth 14 |
    Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Step "Dry-run completed"
Write-Host "Output: $OutputRootFullPath"
Write-Host "Checksums: $checksumPath"
Write-Host "Inspection: $inspectionPath"
Write-Host "Summary: $summaryPath"
