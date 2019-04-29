# Windows Vision Skills DXCoreExtension Documentation

 The experimental *Microsoft.AI.Skills.SkillInterfacePreview.DXCoreExtension* package enables limited VPU support using the prerelease *DXCore* API.

# Microsoft.AI.Skills.SkillInterfacePreview.DXCoreExtension API documentation
+ [Classes](#Classes)
  + [SkillExecutionDeviceDXCore](#SkillExecutionDeviceDXCore)

### SkillExecutionDeviceDirectX <a name="SkillExecutionDeviceDirectX"></a>
``implements`` [Microsoft.AI.Skills.SkillInterfacePreview.ISkillExecutionDevice](../../../doc/Microsoft.AI.Skills.SkillInterfacePreview.md#ISkillExecutionDevice)

Provides a DirectX execution device and its information useful to infer if a skill could be run with it appropriately in the consumer's context. 
Also acts as a static factory for itself.

#### Properties
-----
##### AdapterId

The adapter ID of this DirectX device.

```csharp
long AdapterId { get; }
```
-----

##### DedicatedVideoMemory

The amount of dedicated video memory of this DirectX device (bytes).

```csharp
long DedicatedVideoMemory { get; }
```
-----

##### WinMLDevice

The [LearningModelDevice][LearningModelDevice] associated with this SkillExecutionDevice.

```csharp
Windows.AI.MachineLearning.LearningModelDevice WinMLDevice { get; };
```
-----

#### Methods
-----

##### GetAvailableHardwareExecutionDevices()

Obtain all SkillExecutionDeviceDXCore available on the system so that they can be filtered out appropriately given the 
skill requirements by the skill developer and exposed accordingly when calling 
[ISkillDescriptor.GetSupportedExecutionDevicesAsync()](../../../doc/Microsoft.AI.Skills.SkillInterfacePreview.md#ISkillDescriptor.GetSupportedExecutionDevicesAsync).

```csharp
static IReadOnlyList<SkillExecutionDeviceDXCore> GetAvailableHardwareExecutionDevices();
```

###### Returns
[IReadOnlyList][IReadOnlyList]<[SkillExecutionDeviceDXCore](#SkillExecutionDeviceDXCore)>

All SkillExecutionDeviceDXCore available on the system.

*``Note : This includes only hardware devices and excludes WARP``*

-----


[IReadOnlyList]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2
[LearningModelDevice]: https://docs.microsoft.com/en-us/uwp/api/windows.ai.machinelearning.learningmodeldevice

###### Copyright (c) Microsoft Corporation. All rights reserved.