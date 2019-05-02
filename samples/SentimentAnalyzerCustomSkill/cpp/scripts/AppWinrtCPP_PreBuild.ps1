param($OutDir,$ProjectDir,$WindowsSDK_UnionMetadataPath)

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

$winmdFiles = Get-ChildItem -Path "$OutDir" -Filter *.winmd | %{$_.FullName}
foreach($file in $winmdFiles)
{
    $winrtHeaderScript =  "cppwinrt -in " + "'$file'" + " -ref " + "'$OutDir'" +  " -ref " + "'$WindowsSDK_UnionMetadataPath'" + " -out "+ "'$OutDir'" 
    Invoke-Expression "$winrtHeaderScript"
}
foreach($file in $winmdFiles)
{
    rm $file
}
