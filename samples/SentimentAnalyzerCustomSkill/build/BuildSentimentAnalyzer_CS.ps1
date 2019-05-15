<#
.SYNOPSIS
Performs all steps necessary to build the C# SentimentAnalysis WinRTComponent.
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

Invoke-Expression "$PSScriptRoot\nuget.exe source Add -Name WindowsVisionSkillsLocalBuildFolder -Source $PSScriptRoot"

# Restore packages
RunCommand "$PSScriptRoot\RestorePackages.ps1"
RunCommand "$PSScriptRoot\RestorePackages.ps1 -PackagesConfigPath $PSScriptRoot\..\cs\Skill\FaceSentimentAnalyzer\Contoso.FaceSentimentAnalyzer_CS.csproj"

# Setup the build environement
RunCommand "$PSScriptRoot\SetBuildEnv.ps1"

$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cs\Skill\FaceSentimentAnalyzer\Contoso.FaceSentimentAnalyzer_CS.csproj")

$BuildCommand = $BuildCommandBase + "/m /p:Platform=AnyCPU /p:Configuration=Release"
RunCommand $BuildCommand