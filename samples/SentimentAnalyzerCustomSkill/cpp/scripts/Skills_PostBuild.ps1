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
