<#
.SYNOPSIS
Copies all required binaries to the specified target directory
#>
<#
.DESCRIPTION
-TargetDir: target directory where the app executable is placed
-VCRedistPath: directory containing VC redistributable for the processor architecture specified (i.e.: C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\14.20.27508\redist\x86)
-Debug: optional, if using debug .dll
#>

param($TargetDir,$VCRedistPath,$VCToolsRedistVersion,$PlatformTarget,[switch] $Debug)

echo "Gathering known dependencies..."

$redistfiles =@("vcruntime140" , "msvcp140")
$redistPathToSearch = $VCRedistPath + "Redist\MSVC\" + $VCToolsRedistVersion + "\" + $PlatformTarget
$redistPath = (gci -path $redistPathToSearch -filter "*CRT" | Select-Object -Expand FullName)

if($Debug)
{
    $redistPathDebugToSearch = $VCRedistPath + "Redist\MSVC\" + $VCToolsRedistVersion + "\debug_nonredist\" + $PlatformTarget
	$redistPathDebug = (gci -path $redistPathDebugToSearch -filter "*DebugCRT" | Select-Object -Expand FullName)
    foreach($file in $redistfiles)
    {
        $fullpath = $redistPathDebug + "\" + $file +"d.dll"
        copy -Force $fullpath $TargetDir
    }
}

#*_app.dll symlinks for the skills which are built with the APPCONTAINER flag, assuming skills are all in Release mode only
foreach($file in $redistfiles)
{
    $fullname = $file + ".dll"
    $linkFile = $file + "_app.dll"
    $fullpath = $redistPath + "\" + $fullname
	
    copy -Force $fullpath $TargetDir
    
    # the vc runtime forwarders may not exist for all architectures (i.e. ARM and ARM64), we create the links for the ones that we require
    if(!(Test-Path "$TargetDir$linkFile"))
    {
        cmd /c mklink /h $TargetDir$linkFile $TargetDir$fullname
    }
    
}
