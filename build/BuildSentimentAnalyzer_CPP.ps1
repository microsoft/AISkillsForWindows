<#
.SYNOPSIS
Performs all steps necessary to build the C# SentimentAnalysis WinRTComponent.
#>
<#
.DESCRIPTION
-BuildArch: You can build for a specific architecture (i.e. "All" or "Win32" or "x64" or "ARM", "All" is default)
#>
Param
(
	# Specific architecture to build or all of them (i.e. "All" or "Win32" or "x64" or "ARM")
    [string]$BuildArch = "All"
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

# Restore packages
RunCommand "$PSScriptRoot\RestorePackages.ps1"
RunCommand "$PSScriptRoot\RestorePackages.ps1 -PackagesConfigPath $PSScriptRoot\..\cpp\FaceSentimentAnalyzer\packages.config -PackagesDirectory $PSScriptRoot\..\cpp\packages"

# Setup the build environement
RunCommand "$PSScriptRoot\SetBuildEnv.ps1"

$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cpp\FaceSentimentAnalyzer\FaceSentimentAnalyzer.vcxproj")

if($BuildArch -like "All")
{
    $BuildArchs = @("Win32","x64","ARM")
}
else
{
	$BuildArchs = @($BuildArch)
}

ForEach ($value in $BuildArchs) 
{
    $BuildCommand = $BuildCommandBase + "/m /p:Platform=$value /p:Configuration=Release"
    RunCommand $BuildCommand
}