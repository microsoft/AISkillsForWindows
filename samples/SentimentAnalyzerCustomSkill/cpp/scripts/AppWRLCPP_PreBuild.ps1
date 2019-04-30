param($ProjectDir,$WindowsSDK_UnionMetadataPath)

[xml]$packages = Get-Content "$ProjectDir\packages.config"
#echo $packages.packages
$winmdFiles = @()

foreach ($package in $packages.packages.package)
{
    $searchpath = "$ProjectDir" + "..\packages\" + $package.id + "." + $package.version
    $winmdFiles += Get-ChildItem -Path "$searchpath" -Recurse -Filter *.winmd | %{$_.FullName}
}
$OutDir = "$ProjectDir" + "inc"

if(!(Test-Path "$OutDir"))
{
    mkdir $OutDir 
}
foreach($file in $winmdFiles)
{
    copy $file $OutDir
}

pushd "$OutDir"
$winmdFiles = Get-ChildItem -Path "." -Filter *.winmd -File | %{$_.FullName}
foreach($file in $winmdFiles)
{
    echo "Generating idl for $file"
    $idlgencmd = "winmdidl 2>&1> log.out /nologo /supressversioncheck /outdir:. /metadata_dir:. /metadata_dir:""$WindowsSDK_UnionMetadataPath"" /utf8 ""$file"""
    Invoke-Expression $idlgencmd
}

$idlfilelist = Get-ChildItem -Path "." -Filter *.idl -File | %{$_.FullName}
foreach ($file in $idlfilelist)
{
    echo "Generating headers for $file"
    $headergencmd = "midlrt 2>&1> log.out " + "'$file'" + " /metadata_dir "+ "'$WindowsSDK_UnionMetadataPath'" + " /ns_prefix always" + " /out " + "'$OutDir'"
    Invoke-Expression "$headergencmd"
}
del *.winmd
del *.idl
popd
