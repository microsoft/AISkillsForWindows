param($TargetDir,$WindowsSDK_UnionMetadataPath,$OutDir)

#$TargetDir = $args[0]
#$WinMetadataDir = $args[1]

#for %%x in (*.winmd) do winmdidl %%x /utf8 /metadata_dir:%2 /metadata_dir:. /outdir:.
#for %%x in (*.idl) do midlrt %%x /metadata_dir %2 /ns_prefix always


$filelist = Get-ChildItem -Path "$TargetDir" -Filter *.winmd | %{$_.FullName}
$outdir = "$TargetDir"
#foreach ($file in $filelist)
#{
#echo $file
#    $idlgencmd = "cmd /c winmdidl " + "'$file'" + " /utf8 /metadata_dir:" + "'$WindowsSDK_UnionMetadataPath'" + " /metadata_dir:" + "'$outdir'" + " /outdir:" + "'$outdir'"
#    Invoke-Expression "$idlgencmd" 
#}

$filelist = Get-ChildItem -Path "$TargetDir" -Filter *.idl | %{$_.FullName}
foreach ($file in $filelist)
{
    $headergencmd = "cmd /c midlrt " + "'$file'" + "/metadata_dir "+ "'$WindowsSDK_UnionMetadataPath'" + "/ns_prefix always" + "/out " + "'$outdir'"
    Invoke-Expression "$headergencmd"
}

