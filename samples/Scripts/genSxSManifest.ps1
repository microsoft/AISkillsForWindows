<#
.SYNOPSIS
Generate SxS manifest for a specific .dll using a set of .idl files in a specified project directory.
#>
<#
.DESCRIPTION
arg[0]: name of the target .dll to generate a manifest file for
arg[1]: folder path of where to output the manifest file
arg[2+]: valid .idl file to process
#>

if($args.Count -lt 3)
{
    Write-Output "usage: genSxSManifest.ps1 <dllnameWithoutPath.dll> <pathToOutputManifest> <inputIDLFileWithPath1.idl> <optionalInputIDLFileWithPath2.idl> ....."
    return;
}

$dllFileName = $args[0]
$identity = [io.path]::GetFileNameWithoutExtension($dllFileName);
$outputFile = $args[1]+"\"+$identity+".manifest"

$numIDLs = $args.Count - 2;
$lines =""
while($numIDLs -gt 0)
{
    $idx = 1+$numIDLs;
    $inputFile = $args[$idx]
    Write-Output "Reading $inputFile ...";
    $newlines = Get-Content $inputFile #| Select-String "runtime"
    $lines = $lines + $newlines
    $numIDLs = $numIDLs - 1;
}

write-output "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v3"" manifestVersion=""1.0"">
  <assemblyIdentity
      type=""win32""
      name=""$identity""
      version=""1.0.0.0""/>
  
<file name=""$dllFileName"">" | Out-File -FilePath $outputFile -Encoding ASCII

$currNamespace=""
$scopeCnt = 0;

foreach ($l in $lines)
{
  $t = $l -split("\s+");
  for( $i =0; $i -lt $t.Count; $i++)
  {
      if($t[$i] -like "*{*" -and $currNamespace.Length -gt 0)
      {
        $scopeCnt = $scopeCnt + 1;
      }
      if($t[$i] -like "*}*" -and $currNamespace.Length -gt 0)
      {
        $scopeCnt = $scopeCnt - 1;
      }
      
      if($t[$i].Equals("namespace"))
      {
            if($scopeCnt -eq 0)
            {
                $currNamespace = $t[$i+1] + ".";
            }
            else
            {
                $currNamespace = $currNamespace + $t[$i+1] + ".";
            }
            Write-Output "" "Scanning namespace: $currNamespace";
      } 
      if($t[$i].Equals("runtimeclass")) 
      {
             $class = $currNamespace+ $t[$i+1];
             Write-Output "Found runtimeclass: $class";
             write-output "
          <activatableClass
                name=""$class""
                threadingModel=""both""
                xmlns=""urn:schemas-microsoft-com:winrt.v1"" />" | Out-File -FilePath $outputFile -Append -Encoding ASCII
      }

  }
}
Write-Output "
</file> 
</assembly>" | Out-File -FilePath $outputFile -Append  -Encoding ASCII

