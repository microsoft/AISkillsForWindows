<#
.SYNOPSIS
Performs all steps necessary to build the C# SentimentAnalysis WinRTComponent.
#>
<#
.DESCRIPTION
-BuildArch: You can build for a specific architecture (i.e. "All" or "Win32" or "x64" or "ARM" or "ARM64", "All" is default)
#>
Param
(
	# Specific architecture to build or all of them (i.e. "All" or "Win32" or "x64" or "ARM" or "ARM64")
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

Invoke-Expression "$PSScriptRoot\nuget.exe source Add -Name WindowsVisionSkillsLocalBuildFolder -Source $PSScriptRoot"

# Restore packages
RunCommand "$PSScriptRoot\RestorePackages.ps1"
RunCommand "$PSScriptRoot\RestorePackages.ps1 -PackagesConfigPath $PSScriptRoot\..\cpp\Skill\Obfuscator\packages.config -PackagesDirectory $PSScriptRoot\..\cpp\packages"
RunCommand "$PSScriptRoot\RestorePackages.ps1 -PackagesConfigPath $PSScriptRoot\..\cpp\Skill\Deobfuscator\packages.config -PackagesDirectory $PSScriptRoot\..\cpp\packages"
RunCommand "$PSScriptRoot\RestorePackages.ps1 -PackagesConfigPath $PSScriptRoot\..\cpp\Skill\FaceSentimentAnalyzer\packages.config -PackagesDirectory $PSScriptRoot\..\cpp\packages"

# Setup the build environement
RunCommand "$PSScriptRoot\SetBuildEnv.ps1"

if($BuildArch -like "All")
{
    $BuildArchs = @("Win32","x64","ARM","ARM64")
}
else
{
	$BuildArchs = @($BuildArch)
}

 # Build Obfucation .exe
$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cpp\Skill\Obfuscator\Obfuscator.vcxproj /m /p:Platform=Win32 /p:Configuration=Debug")
RunCommand $BuildCommandBase

# Build DeobfuscationHelper
$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cpp\Skill\Deobfuscator\DeobfuscationHelper.vcxproj")
ForEach ($value in $BuildArchs) 
{
    $BuildCommand = $BuildCommandBase + "/m /p:Platform=$value /p:Configuration=Release"
    RunCommand $BuildCommand
}

# Build skill
$BuildCommandBase = @('msbuild', '--%', "$PSScriptRoot\..\cpp\Skill\FaceSentimentAnalyzer\Contoso.FaceSentimentAnalyzer.vcxproj")
ForEach ($value in $BuildArchs) 
{
    $BuildCommand = $BuildCommandBase + "/m /p:Platform=$value /p:Configuration=Release"
    RunCommand $BuildCommand
}