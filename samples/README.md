# AI Skills for Windows sample guidelines

## System requirements

**Client environement for building C++ and C# skills, C# UWP app:**
- Visual Studio 2017 or later
- Windows 10 build 17763 and related SDK

**Client environement for C++ Win32 Destop app:** 
- Visual Studio 2017 or later
- Windows 10 build 18362 with related SDK
> Sample app projects use pre-build and post-build steps that invoke powershell scripts included in *./Scripts* to generate header files and copy dependent binaries to the target folder.

> **Both C++ and C# .Net Core 3.0 desktop apps** consuming AI Skills for Windows must include an app manifest file that references the manifest file included in each skill NuGet package required. Example of the required section of a manifest file:
```xml
<!-- app.manifest file !-->
...
  <dependency>
    <dependentAssembly>
      <assemblyIdentity
          type="win32"
          name="Microsoft.AI.Skills.SkillInterface"
          version="1.0.0.0"/>
    </dependentAssembly>
  </dependency>

  <dependency>
    <dependentAssembly>
      <assemblyIdentity
          type="win32"
          name="Microsoft.AI.Skills.Vision.SkeletalDetector"
          version="1.0.0.0"/>
    </dependentAssembly>
  </dependency>
...
```

**Client environement for C# .NetCore 3.0 app:** 
- Visual Studio 2019 or later include .NetCore 3.0 
- Windows 10 build 18362 with related SDK

> In the .NetCore 3.0 app project file *\<sample project>.csproj*, you need to ingest the [*Microsoft.Windows.SDK.Contracts* NuGet package](https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts) version 18362 or later that contains the required Windows metadata files (*.winmd*).

## Build the samples
Open the provided solution file: ./VisionSkillsSamples.sln