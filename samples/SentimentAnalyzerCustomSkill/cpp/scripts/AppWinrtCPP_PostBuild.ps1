param($TargetDir,$Arch,$ProjectDir)
[xml]$packages = Get-Content "$ProjectDir\packages.config"
echo "Gathering known dependencies..."

foreach ($package in $packages.packages.package)
{
    $packageroot = "$ProjectDir" + "..\packages\" + $package.id + "." + $package.version
    $dllPath = $packageroot + "\runtimes\win10-" + $Arch
    $manifestPath = $packageroot +"\lib\uap10.0.17763\"
    if(!(Test-Path $manifestPath))
    {
        $manifestPath = $packageroot +"\lib\uap10.0\"
    }
    copy $dllPath\*.dll $TargetDir
    copy $manifestPath\*.manifest $TargetDir

    if(Test-Path "$packageroot\contentFiles")
    {
    
        $contentFiles = Get-ChildItem -Path "$packageroot\contentFiles\" -Recurse -Filter *.* -File | %{$_.FullName}
        foreach($file in $contentFiles)
        {
            copy $file $TargetDir
        }
    }

}
