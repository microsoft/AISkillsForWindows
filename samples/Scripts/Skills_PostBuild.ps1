<#
.SYNOPSIS
Generate SxS manifest for a specific .dll using all .idl files in a specified project directory.
#>
<#
.DESCRIPTION
-TargetName: name of the target .dll to generate a manifest file for
-TargetDir: folder path of where to output the manifest file
-ProjectDir: project directory containig valid .idl file(s) to process
#>

param($TargetName,$TargetDir,$ProjectDir)

$filelist = Get-ChildItem -Path $ProjectDir -Filter *.idl -File | %{$_.FullName}
$fileArg = ""
foreach($file in $filelist)
{
    $fileArg += "'$file'" + " " 
}

$manifestScript = $PSScriptRoot+"\genSxSManifest.ps1"
$OrigArgs = "'$TargetName'" +" " + "'$TargetDir'"

Invoke-Expression "$manifestScript $origArgs $fileArg"
