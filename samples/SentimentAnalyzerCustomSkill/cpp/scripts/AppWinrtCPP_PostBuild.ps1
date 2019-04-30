param($TargetDir,$Arch,$ProjectDir)
[xml]$packages = Get-Content "$ProjectDir\packages.config"
echo "Gathering known dependencies..."

foreach ($package in $packages.packages.package)
{
    $packageroot = "$ProjectDir" + "..\packages\" + $package.id + "." + $package.version
    $dllPath = $packageroot + "\runtimes\win10-" + $Arch
    $dllFiles = Get-ChildItem -Path "$dllPath" -Recurse -Filter *.dll -File | %{$_.FullName}
    foreach($file in $dllFiles) 
    {
        copy $file $TargetDir
    }

    $manifestPath = $packageroot +"\lib\uap10.0.17763\"
    if(!(Test-Path $manifestPath))
    {
        $manifestPath = $packageroot +"\lib\uap10.0\"
    }
    
    $manifestFiles = Get-ChildItem -Path "$manifestPath" -Recurse -Filter *.manifest -File | %{$_.FullName}
    foreach($file in $manifestFiles) 
    {
    
        copy $file $TargetDir
    }

    if(Test-Path "$packageroot\contentFiles")
    {
    
        $contentFiles = Get-ChildItem -Path "$packageroot\contentFiles\" -Recurse -Filter *.* -File | %{$_.FullName}
        foreach($file in $contentFiles)
        {
            copy $file $TargetDir
        }
    }

}
cmd /c mklink $TargetDir\vcruntime140_app.dll c:\windows\system32\vcruntime140.dll
cmd /c mklink $TargetDir\msvcp140_app.dll c:\windows\system32\msvcp140.dll