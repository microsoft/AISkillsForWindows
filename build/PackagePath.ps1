<#
.SYNOPSIS
Returns the path to a NuGet package that matches the version in the root packages.config.

.DESCRIPTION
This script returns the full path to a NuGet package, and ensures it matches the version
in the root packages.config.

By default, this script will attempt to restore packages if it can't find a package at
its expected path. This behavior can be disabled with the -NoRestore switch.
#>
param
(
    # ID of the NuGet package (e.g. Microsoft.Gsl)
    [string]$PackageId,

    # Do not attempt to restore packages if the package cannot be found.
    [switch]$NoRestore
)

$Package = ([xml](Get-Content "$PSScriptRoot\packages.config")).Packages.Package | ? Id -eq $PackageId
if (!$Package)
{
    Write-Warning "Could not find package $PackageId in root packages.config."
    exit 1
}

$PackagePath = "$PSScriptRoot\packages\$($Package.Id).$($Package.Version)"
if (!(Test-Path $PackagePath))
{
    if (!$NoRestore)
    {
        # The package wasn't found, but it may not have been restored yet.
        & "$PSScriptRoot\RestorePackages.ps1"
    }

    if (!(Test-Path $PackagePath))
    {
        Write-Warning "Could not find package $PackageId at '$PackagePath'."
        exit 1
    }
}

(Resolve-Path $PackagePath).Path