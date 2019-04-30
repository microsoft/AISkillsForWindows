param($TargetName,$TargetDir,$ProjectDir,$WindowsSDK_UnionMetadataPath)

$WRLHeaderScript = $PSScriptRoot + "\PostBuildHeaderGen.ps1"
$scriptArgs = "-TargetDir:" + "'$TargetDir'" + " -WindowsSDK_UnionMetadataPath:" + "'$WindowsSDK_UnionMetadataPath'"
$fullcmd = "$WRLHeaderScript" + " " + "$scriptArgs"
Invoke-Expression "$fullcmd" 
