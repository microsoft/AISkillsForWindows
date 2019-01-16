<#
.SYNOPSIS
Performs all steps necessary to build the C# SentimentAnalysis WinRTComponent.
#>
<#
.DESCRIPTION
-BuildArch: You can build for a specific architecture (i.e. "All" or "x86" or "x64" or "ARM", "All" is default)
#>
Param
(
	# Specific architecture to build or all of them (i.e. "All" or "x86" or "x64" or "ARM")
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

# Setup the build environement
RunCommand "$PSScriptRoot\SetBuildEnv.ps1"

$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cs\FaceSentimentAnalyzer\FaceSentimentAnalyzer.csproj")

if($BuildArch -like "All")
{
    $BuildArchs = @("x86", "x64", "ARM")
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