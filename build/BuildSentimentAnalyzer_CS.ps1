<#
.SYNOPSIS
Performs all steps necessary to build the C# SentimentAnalysis WinRTComponent.
#>
<#
.DESCRIPTION
-BuildArch: You can build for a specific architecture (i.e. "AnyCPU" or "x86" or "x64" or "ARM", "AnyCPU" is default)
#>
Param
(
	# Specific architecture to build or all of them (i.e. "AnyCPU" or "x86" or "x64" or "ARM")
    [string]$BuildArch = "AnyCPU"
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

if($BuildArch -like "AnyCPU")
{
    $BuildArchs = @("AnyCPU")
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