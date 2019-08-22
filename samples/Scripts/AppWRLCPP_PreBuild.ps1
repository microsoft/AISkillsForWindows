<#
.SYNOPSIS
Generates temporary .idl files from .winmd referenced via NuGet, then generates headers (.h) files from the previously generated .idl files.
#>
<#
.DESCRIPTION
-GeneratedFilesDir: path to generated file directory
-ProjectDir: path to the folder of the app project that contains package.config detailing dependencies
-PackageDir: path to the folder containing the dependency packages pulled from NuGet
-WindowsSDK_UnionMetadataPath: folder path of the SDK union metadata that contains Windows.winmd (i.e.: C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.18362.0)
#>

param($ProjectDir,$PackageDir,$WindowsSDK_UnionMetadataPath,$GeneratedFilesDir)

[xml]$packages = Get-Content "$ProjectDir\packages.config"
#echo $packages.packages
$winmdFiles = @()

foreach ($package in $packages.packages.package)
{
    $searchpath = $PackageDir + "\packages\" + $package.id + "." + $package.version
    $winmdFiles += Get-ChildItem -Path "$searchpath" -Recurse -Filter *.winmd | %{$_.FullName}
}
$OutDir = "$ProjectDir" + "inc"

if(!(Test-Path "$OutDir"))
{
    echo "Creating directory: $OutDir"
    mkdir $OutDir 
}

echo "Generated Files dir: $GeneratedFilesDir"
$OutDir = "$ProjectDir" + "inc"
$GeneratedFilesDirFullPath = "$ProjectDir" + "$GeneratedFilesDir"
echo "GeneratedFilesDirFullPath: $GeneratedFilesDirFullPath"
if(!(Test-Path "$GeneratedFilesDirFullPath"))
{
    mkdir $GeneratedFilesDirFullPath 
}
else
{
    Get-ChildItem -Path $GeneratedFilesDirFullPath -Recurse | Remove-Item -force -recurse
}

foreach($file in $winmdFiles)
{
    echo "Copy $file to $OutDir"
    copy $file $OutDir
}

pushd "$OutDir"
$winmdFiles = Get-ChildItem -Path "." -Filter *.winmd -File | %{$_.FullName}
foreach($file in $winmdFiles)
{
    echo "Generating idl for $file"
    $idlgencmd = "winmdidl 2>&1> log.out /nologo /supressversioncheck /outdir:. /metadata_dir:. /metadata_dir:""$WindowsSDK_UnionMetadataPath"" /utf8 ""$file"""
    echo "$idlgencmd"
    Invoke-Expression $idlgencmd
}

$idlfilelist = Get-ChildItem -Path "." -Filter *.idl -File | %{$_.FullName}
foreach ($file in $idlfilelist)
{
    echo "Generating headers for $file"
    $headergencmd = "midlrt 2>&1> log.out " + "'$file'" + " /metadata_dir "+ "'$WindowsSDK_UnionMetadataPath'" + " /ns_prefix always" + " /out " + "'$OutDir'"
    echo "$headergencmd"
    Invoke-Expression "$headergencmd"
}

echo "copy .\* $GeneratedFilesDirFullPath"
copy $OutDir\* $GeneratedFilesDirFullPath\winrt

del *.winmd
del *.idl
popd
