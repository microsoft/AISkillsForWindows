<#
.SYNOPSIS
Configures a PowerShell environment for building with Visual Studio.
#>

# Skip if msbuild is already on the PATH.
if (Get-Command -Name msbuild -ErrorAction Ignore)
{
    exit 0
}

# VSWhere is used to find the VS installation directory.
$VSWherePackageLocation = (& "$PSScriptRoot\PackagePath.ps1" vswhere)
if (!$VSWherePackageLocation)
{
    Write-Error 'Could not find the vswhere package.'
    exit 1
}

Write-Host "Found package " + "$VSWherePackageLocation"

$VSWherePath = Join-Path $VSWherePackageLocation 'tools\vswhere.exe'
if (!$VSWherePath)
{
    Write-Warning 'Could not find vswhere.exe.'
    exit 1
}

# Run vswhere.exe to get path to VS installation.
$VSPath = ((& $VSWherePath -latest -format json) | ConvertFrom-Json).InstallationPath | Select-Object -First 1

# Set VS variables for building on the command line.
cmd /c "`"$VSPath\Common7\Tools\VsDevCmd.bat`" & set" | 
    Where-Object { $_ -match '=' } | 
    ForEach-Object { $v = $_.split('='); Set-Item -Path "env:$($v[0])" -Value $v[1] -Force }

# Add WindowsTools to the path.
if ($env:PATH -notmatch [regex]::Escape($ToolsPath))
{
    $env:PATH += ";$ToolsPath"
}