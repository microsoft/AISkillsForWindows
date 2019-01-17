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
RunCommand "$PSScriptRoot\RestorePackages.ps1 -PackagesConfigPath $PSScriptRoot\..\cs\FaceSentimentAnalysis_CS.sln"

# Setup the build environement
RunCommand "$PSScriptRoot\SetBuildEnv.ps1"

$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cs\FaceSentimentAnalyzer\Contoso.FaceSentimentAnalyzer.csproj")

$BuildCommand = $BuildCommandBase + "/m /p:Platform=AnyCPU /p:Configuration=Release"
RunCommand $BuildCommand