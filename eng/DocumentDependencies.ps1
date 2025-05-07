#
# This script generates the THIRD-PARTY-NOTICES.md files from GitHub's SBOM dependency graph and NuGet package information.
#

# Parse script arguments
param (
    [switch]$NoContent
)

$sourceRepoUrl = "https://github.com/metalama/Metalama"


# Global dictionary to override package information
$global:PackageOverrides = @{
    "ComparerExtensions" = @{
        License = "Public domain"
    }
    "ILRepack" = @{
        License = "Apache-2.0"
    }
    "LibGit2Sharp" = @{
        License = "MIT"
    }
    "LibGit2Sharp.NativeBinaries" = @{
        License = "MIT"
    }
    "LINQPad.Reference" = @{
        License = "Proprietary"
    }
    "LINQPad.Runtime" = @{
        License = "Proprietary"
    }
    "xunit" = @{
        License = "Apache-2.0"
        SourceRepository = "https://github.com/xunit/xunit"
    }
    "xunit.core" = @{
        License = "Apache-2.0"
        SourceRepository = "https://github.com/xunit/xunit"
    }
    "xunit.extensibility.core" = @{
        SourceRepository = "https://github.com/xunit/xunit"
    }
    "xunit.extensibility.execution" = @{
        SourceRepository = "https://github.com/xunit/xunit"
    }
    "xunit.runner.visualstudio" = @{
        SourceRepository = "https://github.com/xunit/visualstudio.xunit"
    }
    "xunit.assert" = @{
        SourceRepository = "https://github.com/xunit/assert.xunit"
    }
    "xunit.analyzers" = @{
        SourceRepository = "https://github.com/xunit/xunit.analyzers"
    }
    "xunit.abstractions" = @{
        License = "Apache-2.0"
    }
    "FakeItEasy" = @{
        SourceRepository = "https://github.com/FakeItEasy/FakeItEasy"
    }
    "FakeItEasy.Analyzer.CSharp" =  @{
        SourceRepository = "https://github.com/FakeItEasy/FakeItEasy"
    }
    "JetBrains.Annotations" = @{
        SourceRepository = "https://github.com/JetBrains/JetBrains.Annotations"
    }
    "Typesense" = @{
        SourceRepository = "https://github.com/DAXGRID/typesense-dotnet"
    }
    "K4os.Hash.xxHash" = @{
        License = "MIT"
    }
    "Octokit.GraphQL" = @{
        License = "MIT"
    }
    "CommandLineParser" = @{
        License = "MIT"
    }
    "HtmlAgilityPack" = @{
        SourceRepository = "https://github.com/zzzprojects/html-agility-pack/"
    }
    "Castle.Core" = @{
        SourceRepository = "https://github.com/castleproject/Core"
    }
    "Validation" = @{
        SourceRepository = "https://github.com/aarnott/Validation"
    }
    "NuGet.Versioning" = @{
        SourceRepository = "https://github.com/NuGet/Home"
    }
    "Newtonsoft.Json" = @{
        SourceRepository = "https://github.com/JamesNK/Newtonsoft.Json"
    }
}

# Package name patterns to skip
$global:PackageNameExclusions = @(
    "System.*",
    "Azure.*",
    "WindowsAzure.*",
    "Microsoft.*",
    "NETStandard.*"
)

# Package usage patterns
$global:PackageUsagePatterns = @{
    "PostSharp.Engineering.*" = "Building Metalama"
    "coverlet*" = "Testing Metalama"
    "xunit*" = "Testing Your Aspects"
    "FakeItEasy*" = "Testing Metalama"
    "Diff*" = "Testing Metalama"
    "ComparerExtensions*" = "Testing Metalama"
    "Benchmark*" = "Testing Metalama"
    "LINQPad.*" = "Using LINQPad"
    "CommunityToolkit.Mvvm" = "Testing Metalama"
    # FluentAssertions is being replaced by AwesomeAssertions in Metalama but the dependency graph still shows it on GitHub.
    "FluentAssertions" = "Testing Metalama"
    "AwesomeAssertions" = "Testing Metalama"
}

# Download the SBOM JSON file
$sbomUrl = "$sourceRepoUrl/dependency-graph/sbom"
$tempFile = "$env:TEMP\sbom.json"
Invoke-WebRequest -Uri $sbomUrl -OutFile $tempFile -ErrorAction Stop
$sbom = Get-Content -Path $tempFile | ConvertFrom-Json


# Function to apply overrides from the global dictionary
function Apply-PackageOverrides {
    param (
        [string]$packageName,
        [hashtable]$packageInfo
    )

    if ($global:PackageOverrides.ContainsKey($packageName)) {
        $overrides = $global:PackageOverrides[$packageName]
        if ( $overrides.License ) {
            $packageInfo.License = $overrides.License
        }

        if ( $overrides.SourceRepository ) {
            $packageInfo.SourceRepository = $overrides.SourceRepository
        }
        
    }
}

# Function to fetch and add license, owner information, source repository, and usage type
function Discover-PackageInfo {
    param (
        $packageName,
        [hashtable]$packageInfoMap,
        [string]$usage = $null,
        [string]$discoveredBy = $null
    )
    # Skip dependencies based on the global exclusion list
    foreach ($exclusion in $global:PackageNameExclusions) {
        if ($packageName -like $exclusion) {
            Write-Host "Skipping $packageName due to exclusion pattern."
            return
        }
    }
   
    if ($packageInfoMap.ContainsKey($packageName)) {
        # Add to the DiscoveredBy HashSet if this dependency is already processed
        $packageInfoMap[$packageName].DiscoveredBy.Add($discoveredBy)
        # Add to the Usage HashSet if this dependency is already processed
        if ($usage) {
            $packageInfoMap[$packageName].Usage.Add($usage)
        }
        return
    }

    # Determine usage type (use provided usage if available, otherwise infer)
    $finalUsage = if ($usage) {
        $usage
    } else {
        foreach ($pattern in $global:PackageUsagePatterns.Keys) {
            if ($packageName -like $pattern) {
                $global:PackageUsagePatterns[$pattern]
                break
            }
        }
    }

    if (-not $finalUsage) {
        $finalUsage = "Building Your Product"
    }

    $baseUrl = "https://api.nuget.org/v3/registration5-gz-semver2/$($packageName.ToLower())/index.json"
    $retryCount = 0
    $maxRetries = 10

    while ($retryCount -lt $maxRetries) {
        try {
            Start-Sleep -Seconds 2
            # First request to fetch the pages
            Write-Host "Fetching $baseUrl..."
            $response = Invoke-WebRequest -Uri $baseUrl -ErrorAction Stop
            $content = $response.Content | ConvertFrom-Json

            # Ensure the structure is valid
            if (-not $content.items) {
                Write-Host $response.Content
                throw "Invalid response structure from NuGet API for $packageName"
            }

            if ($content.items[-1].items) {
                $catalogEntry = $content.items[-1].items[-1].catalogEntry
            } else {
                # Fetch the last page URL
                $lastPageUrl = $content.items[-1]."@id"
                if (-not $lastPageUrl) {
                    throw "Cannot find the last page URL for $packageName"
                }

                Write-Host "Fetching $lastPageUrl..."

                # Second request to fetch the catalog entry from the last page
                $lastPageResponse = Invoke-WebRequest -Uri $lastPageUrl -ErrorAction Stop
                $lastPageContent = $lastPageResponse.Content | ConvertFrom-Json

                $catalogEntry = $lastPageContent.items[-1].catalogEntry
                if (-not $catalogEntry) {
                    Write-Host $lastPageContent.items[-1]
                    throw "Cannot find the catalogEntry in $lastPageUrl"
                }
            }

            # Extract license information
            $license = if ($catalogEntry.licenseExpression) {
                $catalogEntry.licenseExpression
            } else {
                $null
            }

            # Extract owner information
            $owners = if ($catalogEntry.authors) {
                $catalogEntry.authors -join ", "
            } elseif ($packageName -like "Microsoft*") {
                "Microsoft"
            } else {
                $null
            }

            # Extract source repository URL
            $sourceRepo = if ($catalogEntry.projectUrl) {
                $catalogEntry.projectUrl
            } else {
                $null
            }

            # Add to the dictionary
            $packageInfoMap[$packageName] = @{
                License = $license
                Owners = $owners
                SourceRepository = $sourceRepo
                Usage = [System.Collections.Generic.HashSet[string]]::new()
                DiscoveredBy = [System.Collections.Generic.HashSet[string]]::new()
            }
            $packageInfoMap[$packageName].Usage.Add($finalUsage)
            $packageInfoMap[$packageName].DiscoveredBy.Add($discoveredBy ? $discoveredBy : "Direct")

            # Apply overrides if any
            Apply-PackageOverrides -packageName $packageName -packageInfo $packageInfoMap[$packageName]

            # Skip deep-processing for "Building Metalama" dependencies
            #if ($finalUsage -eq "Building Metalama") {
            #    return
            #}

            # Process recursive dependencies
            if ($catalogEntry.dependencyGroups) {
                foreach ($dependencyGroup in $catalogEntry.dependencyGroups) {
                    if ($dependencyGroup.dependencies) {
                        foreach ($dependency in $dependencyGroup.dependencies) {
                            Write-Host "Discovered dependency: $($dependency.id)..."
                            Discover-PackageInfo -packageName $dependency.id -packageInfoMap $packageInfoMap -usage $finalUsage -discoveredBy $packageName
                        }
                    }
                }
            }

            return # Exit loop on success
        } catch {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Warning "Failed to fetch package information for $packageName from $baseUrl. Retrying in 15 seconds... ($retryCount/$maxRetries). Exception: $($_.Exception.Message)"
                Start-Sleep -Seconds 15
            } else {
                Write-Warning "Failed to fetch package information for $packageName from $baseUrl after $maxRetries attempts. Exception: $($_.Exception.Message)"
                $packageInfoMap[$packageName] = @{
                    License = "Cannot find package $baseUrl"
                    Owners = if ($packageName -like "Microsoft*") { "Microsoft" } else { "Cannot find owners" }
                    SourceRepository = "Cannot find source repository"
                    Usage = [System.Collections.Generic.HashSet[string]]::new()
                    DiscoveredBy = [System.Collections.Generic.HashSet[string]]::new()
                }
                if ($usage) {
                    $packageInfoMap[$packageName].Usage.Add($usage)
                }
                $packageInfoMap[$packageName].DiscoveredBy.Add($discoveredBy)
            }
        }
    }
}

# Global variable to store license and notice content


$global:LicenseNoticesText = @()

# Function to format Markdown titles to third-level titles
function Format-MarkdownTitles {
    param (
        [string]$content
    )
    # Replace Markdown titles (#, ##) with ###
    return ($content -replace '^(#+)', '###')
}

# Function to unindent content by removing the minimum leading spaces
function Unindent-Content {
    param (
        [string]$content
    )
    $lines = $content -split "`n"
    $minIndent = ($lines | Where-Object { $_ -match '\S' } | ForEach-Object { ($_ -match '^\s*')[0].Length }) | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum
    return ($lines | ForEach-Object { $_ -replace "^\s{$minIndent}" }).TrimEnd() -join "`n"
}

# Function to fetch license and notice files from a GitHub repository
function Fetch-LicenseAndNoticeFiles {
    param (
        [string]$repoUrl
    )

    # Extract organization and project name from the repository URL
    if ($repoUrl -match "https://github.com/([^/]+)/([^/]+)") {
        $org = $matches[1]
        $project = $matches[2]
        $apiUrl = "https://api.github.com/repos/$org/$project/contents/"
        Write-Host "Fetching file list from $apiUrl..."

        $retryCount = 0
        $maxRetries = 3

        while ($retryCount -lt $maxRetries) {
            try {
                # Fetch the list of files in the repository
                Start-Sleep -Seconds 2
                $response = Invoke-WebRequest -Uri $apiUrl -Headers @{ "User-Agent" = "PowerShell" } -ErrorAction Stop
                $files = $response.Content | ConvertFrom-Json

                # Define the list of license and notice file names to search for
                $targetFiles = @("LICENSE", "LICENSE.MD", "LICENSE.TXT", "NOTICE", "NOTICE.MD", "NOTICE.TXT")

                # Filter files based on the target list (case-insensitive)
                $matchingFiles = $files | Where-Object { $targetFiles -contains $_.name.ToUpper() }

                if ($matchingFiles) {
                    $global:LicenseNoticesText += "`n`n"
                    $global:LicenseNoticesText += "---"
                    $global:LicenseNoticesText += "`n`n"

                    $global:LicenseNoticesText += "## License notices for $project"
                    $global:LicenseNoticesText += "`n"

                    foreach ($file in $matchingFiles) {
                        Start-Sleep -Seconds 2
                        $fileContent = Invoke-WebRequest -Uri $file.download_url -Headers @{ "User-Agent" = "PowerShell" } -ErrorAction Stop
                        $content = $fileContent.Content

                        # Format Markdown titles if the file is a Markdown file
                        if ($file.name -match "\.MD$") {
                            $content = Format-MarkdownTitles -content $content
                        }

                        # Unindent the content
                        $content = Unindent-Content -content $content

                        # Quote all lines with `> `
                        $quotedContent = ($content -split "`n" | ForEach-Object { "> $_" }) -join "`n"

                        $global:LicenseNoticesText += $quotedContent
                        Write-Host "Appended: $($file.name)"
                    }
                } else {
                    Write-Host "No license or notice files found for $project."
                }

                return # Exit loop on success
            } catch {
                if ($_.Exception.Response.StatusCode -eq 403) {
                    $retryCount++
                    if ($retryCount -lt $maxRetries) {
                        Write-Warning "HTTP 403 encountered while fetching $apiUrl. Retrying in 300 seconds... ($retryCount/$maxRetries)."
                        Start-Sleep -Seconds 300
                    } else {
                        Write-Warning "Failed to fetch file list from $apiUrl after $maxRetries attempts due to HTTP 403."
                    }
                } else {
                    Write-Warning "Failed to fetch license and notice files for $repoUrl. Exception: $($_.Exception.Message)"
                    break
                }
            }
        }
    } else {
        Write-Warning "Invalid GitHub repository URL: $repoUrl"
    }
}

# Extract, filter, and sort dependency names
Write-Host "Dependencies (NuGet only, excluding System and Metalama namespaces):"
$dependencies = $sbom.packages | Where-Object {
    $_.SPDXID -like "SPDXRef-nuget-*" -and -not (   $_.name -like "System.*" -or $_.name -like "Metalama.*" )
}

# Build a dictionary mapping package names to their information
Write-Host "Fetching package information..."
$packageInfoMap = @{}
$dependencies | ForEach-Object {
    Write-Host "Processing package: $($_.name)..."
    Discover-PackageInfo -packageName $_.name -packageInfoMap $packageInfoMap 
}

# Fetch and append license and notice files for each distinct repository
if (-not $NoContent) {
    Write-Host "Fetching license and notice files from GitHub repositories..."
    $distinctRepos = $packageInfoMap.GetEnumerator() | Select-Object -ExpandProperty Value | Select-Object -ExpandProperty SourceRepository -Unique | Sort-Object -Unique
    foreach ($repoUrl in $distinctRepos) {
        if ( $repoUrl -like "https://github.com/*" ) {
            Fetch-LicenseAndNoticeFiles -repoUrl $repoUrl
        } else {
            Write-Warning "Skipping non-GitHub repository: $repoUrl"
        }

    }
} else {
    Write-Host "Skipping fetching license and notice files (--no-content specified)."
}

# Write the Markdown table and license/notice content to THIRD-PARTY-NOTICES.md
$thirdPartyNoticesFile = "$PSScriptRoot/../THIRD-PARTY-NOTICES.md"
Write-Host "Writing $thirdPartyNoticesFile..."

# Write the Markdown table
@"
# Third-Party Notices

This file lists and documents the licenses and notices for third-party dependencies of the Metalama.

> [!WARNING]
> This file pertains to the whole [Metalama repository](https://github.com/metalama/Metalama). If you find this file in a NuGet package, it does not mean that this package has all the dependencies listed here. Please check the dependencies at [NuGet.org](https://www.nuget.org/packages?q=metalama) for the actual dependencies of the package.

> [!WARNING]
> In the event that we accidentally failed to list a required notice, please bring it to our attention. Post an issue or email us at <hello@postsharp.net>.

This file is semi-automatically generated by [DocumentDependencies.ps1](eng/DocumentDependencies.ps1).

## List of NuGet Dependencies

Metalama has a large number of dependencies, but few flow to the end-user of your products. 

To simplify your impact assessment, we have grouped them into the following categories:

- **Building Metalama**: These dependencies are used to build Metalama itself. They do not flow with your packages.
- **Building Your Product**: These dependencies are used when building your projects. You can typically remove them form 
  your package by marking them as private assets.
- **Testing**: These dependencies are used to test either Metalama or your aspects. They do not flow with your product.
- **LINQPad**: Only when using LINQPad.

> [!NOTE]
> For brevity, this list does not include dependencies that are part of the ``System``, ``Microsoft`` or ``Azure`` namespaces.


| Package Name | License | Authors | Source Repository | Usage | Referenced By |
|--------------|---------|--------|-------------------|-------|---------------|
"@ | Out-File -FilePath $thirdPartyNoticesFile -Encoding UTF8

$packageInfoMap.GetEnumerator() | Sort-Object -Property Key | ForEach-Object {
    $packageName = $_.Key
    $packageInfo = $_.Value
    $packageUrl = "https://www.nuget.org/packages/$packageName"
    $usage = $packageInfo.Usage -join ", "
        $discoveredBy = ($packageInfo.DiscoveredBy | Sort-Object) -join ", "

    # Format Owners as a Markdown hyperlink if it is a URL
    $owners = if ($packageInfo.Owners -match "^https?://") {
        "[Contributors]($($packageInfo.Owners))"
    } else {
        $packageInfo.Owners
    }

    # Format Source Repository as a Markdown hyperlink with the project name as text
    if ($packageInfo.SourceRepository -match "https://github.com/([^/]+)/([^/]+)") {
        $org = $matches[1]
        $project = $matches[2]
        $sourceRepo = "[$($project)]($($packageInfo.SourceRepository))"
    } else {
        $sourceRepo = $packageInfo.SourceRepository
    }

    "| [$packageName]($packageUrl) | $($packageInfo.License) | $owners | $sourceRepo | $usage | $discoveredBy |" | Out-File -Append -FilePath $thirdPartyNoticesFile -Encoding UTF8
}

# Write the license and notice content
$global:LicenseNoticesText | Out-File -Append -FilePath $thirdPartyNoticesFile -Encoding UTF8

Write-Host "THIRD-PARTY-NOTICES.md has been generated."

# Clean up the temporary file
Remove-Item -Path $tempFile -Force
