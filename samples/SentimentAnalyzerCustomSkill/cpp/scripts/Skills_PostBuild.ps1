param($TargetName,$TargetDir,$ProjectDir)

$filelist = Get-ChildItem -Path $ProjectDir -Filter *.idl | %{$_.FullName}
$fileArg = $filelist -join ' '
$manifestScript = $PSScriptRoot+"\genSxSManifest.ps1"
$OrigArgs = $TargetName +" " + $TargetDir

Invoke-Expression "$manifestScript $origArgs $fileArg"
