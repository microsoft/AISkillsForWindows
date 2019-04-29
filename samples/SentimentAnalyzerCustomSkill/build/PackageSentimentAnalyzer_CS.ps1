<#
.SYNOPSIS
Package all Skills.
#>
Param
(
    # Location to generate build files.
    [string]$BuildDirectory = "$PSScriptRoot\..",

    # Path to nuget.exe.
    [string]$NugetPath = "$PSScriptRoot\nuget.exe"
)

function RunCommand($Command)
{
    Write-Host "$Command"
    Invoke-Expression "$Command"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Warning 'Error encountered. Aborting.'
        exit 1
    }
}

$PackCommandBase = @($NugetPath)
$PackCommandBase += "pack"

$PackCommand = $PackCommandBase + "$PSScriptRoot\Contoso.FaceSentimentAnalyzer_CS.nuspec"
RunCommand $PackCommand