<#
.SYNOPSIS
Restores all NuGet packages used by the project.

.DESCRIPTION
This script is part of the automated build script.
However, you can call it independently to restore all nuget packages used by project.
#>
param
(
    # Optional path to nuget.exe. If not specified, nuget.exe will be downloaded.
    [string]$NugetPath = "$PSScriptRoot\nuget.exe",

    # Path to packages.config, which lists the packages to restore.
    [string]$PackagesConfigPath = "$PSScriptRoot\packages.config",

    # Path to extract packages.
    [string]$PackagesDirectory = "$PSScriptRoot\packages"
)

Push-Location ($PackagesConfigPath | Split-Path -Parent)
& $NugetPath restore -NonInteractive $PackagesConfigPath -PackagesDirectory $PackagesDirectory
Pop-Location
if ($LASTEXITCODE -ne 0)
{
    Write-Warning 'Errors restoring nuget packages.'
    exit 1
}