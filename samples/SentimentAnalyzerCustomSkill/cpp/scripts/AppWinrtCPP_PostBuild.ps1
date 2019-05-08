param($TargetDir,$Arch,$ProjectDir,$VCInstallDir,[switch] $Debug)

echo "Gathering known dependencies..."

$redistfiles =@("vcruntime140" , "msvcp140")
$redistPath = $VCInstallDir + "Redist\MSVC\14.16.27012\onecore\$Arch\Microsoft.VC141.CRT\"
if($Debug)
{
    $redistPathDebug = $VCInstallDir + "Redist\MSVC\14.16.27012\onecore\debug_nonredist\$Arch\Microsoft.VC141.DebugCRT\"
    foreach($file in $redistfiles)
    {
        $fullpath = $redistPathDebug + $file +"d.dll"
        copy -Force $fullpath $TargetDir
    }
}

#*_app.dll symlinks for the skills which are built with the APPCONTAINER flag, assuming skills are all in Release mode only
foreach($file in $redistfiles)
{
    $fullname = $file + ".dll"
    $linkFile = $file + "_app.dll"
    $fullpath = $redistPath+$fullname
    copy -Force $fullpath $TargetDir
    
    # the vc runtime forwarders may not exist for all architectures, we create the links for the ones that we require
    if(!(Test-Path "$TargetDir$linkFile"))
    {
        cmd /c mklink /h $TargetDir$linkFile $TargetDir$fullname
    }
    
}
