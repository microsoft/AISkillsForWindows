# Windows Vision Skill sample guidelines

## System requirements

**Client environement for building C++ and C# skills, C# UWP app:**
- Visual Studio 2017 or later
- Windows 10 build 17763 and related SDK

**Client environement for C++ Win32 Destop app:** 
- Visual Studio 2017 or later
- Windows 10 build 18362 with related SDK
> Sample app projects use pre-build and post-build steps that invoke powershell scripts included in *./Scripts* to generate header files and copy dependent binaries to the target folder.

> **Both C++ and C# .Net Core 3.0 desktop apps** consuming Windows vision skills must include an app manifest file that references the manifest file included in each skill NuGet package required. Example of the required section of a manifest file:
```xml
<!-- app.manifest file !-->
...
  <dependency>
    <dependentAssembly>
      <assemblyIdentity
          type="win32"
          name="Microsoft.AI.Skills.SkillInterfacePreview"
          version="1.0.0.0"/>
    </dependentAssembly>
  </dependency>

  <dependency>
    <dependentAssembly>
      <assemblyIdentity
          type="win32"
          name="Microsoft.AI.Skills.Vision.SkeletalDetectorPreview"
          version="1.0.0.0"/>
    </dependentAssembly>
  </dependency>
...
```

**Client environement for C# .NetCore 3.0 app:** 
- Visual Studio 2019 or later with .NetCore preview enabled (*Tools* → *Options* → *Project and Solutions* → *.NET Core* and then check *Use previews of the .NET Core SDK*)
- Windows 10 build 18362 with related SDK
- [.NetCore 3.0 preview](https://dotnet.microsoft.com/download/dotnet-core/3.0) installed and enabled(follow instructions)

> In the .NetCore 3.0 app project file *\<sample project>.csproj*, some assumption are made as to where the required Windows metadata files (*.winmd*) are stored on your computer (see ***HintPath*** below), double check they are correct if you encouter an error. Example of a .csproj with the aforementioned assumptions:
```xml
<ItemGroup>
    <Reference Include="System.Runtime.WindowsRuntime">
    <HintPath>C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll</HintPath>
    </Reference>
    <Reference Include="Windows">
    <HintPath>C:\Program Files (x86)\Windows Kits\10\UnionMetadata\Facade\Windows.WinMD</HintPath>
    <IsWinMDFile>true</IsWinMDFile>
    </Reference>
    <Reference Include="Windows.Foundation.FoundationContract">
    <HintPath>C:\Program Files (x86)\Windows Kits\10\References\10.0.17763.0\Windows.Foundation.FoundationContract\3.0.0.0\Windows.Foundation.FoundationContract.winmd</HintPath>
    <IsWinMDFile>true</IsWinMDFile>
    </Reference>
    <Reference Include="Windows.Foundation.UniversalApiContract">
    <HintPath>C:\Program Files (x86)\Windows Kits\10\References\10.0.17763.0\Windows.Foundation.UniversalApiContract\7.0.0.0\Windows.Foundation.UniversalApiContract.winmd</HintPath>
    <IsWinMDFile>true</IsWinMDFile>
    </Reference>
</ItemGroup>
```

## Build the samples
Open the provided solution file: ./VisionSkillsSamples.sln