<#
.SYNOPSIS
Generates headers (.h) files from a set of referenced .winmd files.
#>
<#
.DESCRIPTION
-OutDir: output folder path to place the generated header (.h) files
-ProjectDir: path to the folder of the app project that contains package.config detailing dependencies
-PackageDir: path to the folder containing the dependency packages pulled from NuGet
-WindowsSDK_UnionMetadataPath: folder path of the SDK union metadata that contains Windows.winmd (i.e.: C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.18362.0)
#>

param($OutDir,$ProjectDir,$PackageDir,$WindowsSDK_UnionMetadataPath)

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
    mkdir $OutDir 
}
foreach($file in $winmdFiles)
{
    copy $file $OutDir
}

$winmdFiles = Get-ChildItem -Path "$OutDir" -Filter *.winmd | %{$_.FullName}
foreach($file in $winmdFiles)
{
	echo "Generating headers for $file"
    $winrtHeaderScript =  "cppwinrt -in " + "'$file'" + " -ref " + "'$OutDir'" +  " -ref " + "'$WindowsSDK_UnionMetadataPath'" + " -out "+ "'$OutDir'" 
    Invoke-Expression "$winrtHeaderScript"
}
foreach($file in $winmdFiles)
{
    rm $file
}
