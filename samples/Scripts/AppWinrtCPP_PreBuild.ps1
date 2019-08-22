<#
.SYNOPSIS
Generates headers (.h) files from a set of referenced .winmd files.
#>
<#
.DESCRIPTION
-GeneratedFilesDir: path to generated file directory
-ProjectDir: path to the folder of the app project that contains package.config detailing dependencies
-PackageDir: path to the folder containing the dependency packages pulled from NuGet
-WindowsSDK_UnionMetadataPath: folder path of the SDK union metadata that contains Windows.winmd (i.e.: C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.18362.0)
#>

param($ProjectDir,$PackageDir,$WindowsSDK_UnionMetadataPath,$GeneratedFilesDir="")

[xml]$packages = Get-Content "$ProjectDir\packages.config"
$winmdFiles = @()
$cppWinRTExecPath = $null;

foreach ($package in $packages.packages.package)
{
    echo ($package.id + "." + $package.version)
    $searchpath = $PackageDir + "\packages\" + $package.id + "." + $package.version
    $winmdFiles += Get-ChildItem -Path "$searchpath" -Recurse -Filter *.winmd | %{$_.FullName}
    if($package.id.Contains('CppWinRT'))
    {
        $cppWinRTExecPath += Get-ChildItem -Path "$searchpath" -Recurse -Filter "cppwinrt.exe" | %{$_.FullName}
    }
}

if($cppWinRTExecPath)
{
    echo ("Found cppwinrt executable: " + $cppWinRTExecPath)
}
else
{
    throw 'Could not find cppwinrt.exe'
}

$OutDir = "$ProjectDir" + "inc"

if($GeneratedFilesDir -ne "")
{
    echo "Generated Files dir: $GeneratedFilesDir"
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
}
if(!(Test-Path "$OutDir"))
{
    mkdir $OutDir 
}
foreach($file in $winmdFiles)
{
    echo "copying $file to $OutDir"
    copy $file $OutDir
}

$winmdFiles = Get-ChildItem -Path "$OutDir" -Filter *.winmd | %{$_.FullName}
foreach($file in $winmdFiles)
{
    echo "Generating headers for $file"
    $winrtHeaderScript =  "$cppWinRTExecPath -in " + "'$file'" + " -ref " + "'$OutDir'" +  " -ref " + "'$WindowsSDK_UnionMetadataPath'" + " -out '$OutDir'" 
    echo $winrtHeaderScript
    Invoke-Expression "$winrtHeaderScript"
}

if($GeneratedFilesDir -ne "")
{
    echo "copy $OutDir\winrt\* $GeneratedFilesDirFullPath\winrt"
    copy $OutDir\winrt\* $GeneratedFilesDirFullPath\winrt
}

foreach($file in $winmdFiles)
{
    echo "removing $file"
    rm $file
}
