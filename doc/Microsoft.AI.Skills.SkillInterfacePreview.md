 # Windows Skill Interface Documentation

 ### Summary

Implementing and integrating efficient AI and Computer Vision (CV) solutions is a hard task for developers. The industry is moving at a fast pace and the amount of custom-tailored 
solutions are coming out and app developer simply cannot keep-up easily. This proposed framework is meant to standardize the way AI and CV is put to use within a WinRT application 
running on the edge. It aims to abstract away the complexity of AI and CV techniques by simply defining the concept of Skills which are modular pieces of code that process input and 
produce output. The implementation that contains the complex details is encapsulated by an extensible WinRT API that inherits the base class present in this namespace, which leverages 
built-in Windows primitives which in-turn eases interop with built-in acceleration frameworks or external 3rd party ones.

 The *Microsoft.AI.Skills.SkillInterfacePreview* namespace provides a set of base interfaces to be extended by all skills as well as classes and helper methods for skill implementers 
 on Windows.

 ### Important concepts
 
 This API is meant to streamline the way *skills* work and are interacted with by developers. In that optic, each skill implements the following interfaces: 
 1. **[ISkillDescriptor](#ISkillDescriptor)** derivative that provides information on the skill and exposes its requirements (input, output, version, etc).
 2. **[ISkillBinding](#ISkillBinding)** derivative that serves as a dictionary and a conduit for input and output variables to be passed back and forth to the
 **[ISkill](#ISkill)**.
 It handles pre and post processing of the input/output data and simplifies their access.
 3. **[ISkill](#ISkill)** derivative that exposes the core logic of processing its input and forming its output via an **[ISkillBinding](#ISkillBinding)**.

 Skills are meant to leverage optimally the hardware capabilities (*CPU*, *GPU*, etc.) on each system they run on. Therefore skills have 
 to expose the set of [**ISkillExecutionDevice**](#ISkillExecutionDevice)s currently available and filter them into a list of compatible ones with their skill logic 
 so that the developer consuming their skill can best chose how to tap into supported hardware resources at runtime on a user system. 

 In order to ensure that input and output variables are passed in the correct format to the skill, the interface defines 
 **[ISkillFeature](#ISkillFeature)**, which is meant to encapsulate a value in a predefined format. This value is represented by a **[ISkillFeatureValue](#ISkillFeatureValue)**, 
from which this interface also defines multiple common derivatives of (tensor, image, string, etc.). For these derivatives, automatic 
 conversion to the appropriate format occurs when attempting to set a value that does not match the predefined format (i.e. when binding an image
in a different format than the one required).

# Microsoft.AI.Skills.SkillInterfacePreview API doc

*For code see: [Microsoft.AI.Skills.SkillInterface.idl](../common/VisionSkillBase/Microsoft.AI.Skills.SkillInterface.idl)*

+ [Enums](#Enums)
  + [SkillFeatureKind](#SkillFeatureKind)
  + [SkillExecutionDeviceKind](#SkillExecutionDeviceKind)
  + [SkillElementKind](#SkillElementKind)
  + [D3DFeatureLevelKind](#D3DFeatureLevelKind)
+ [Interfaces](#Interfaces)
  + [ISkillExecutionDevice](#ISkillExecutionDevice)
  + [ISkillFeature](#ISkillFeature)
  + [ISkillFeatureValue](#ISkillFeatureValue)
  + [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)
  + [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)
  + [ISkillFeatureMapDescriptor](#ISkillFeatureMapDescriptor)
  + [ISkillFeatureImageDescriptor](#ISkillFeatureImageDescriptor)
  + [ISkillDescriptor](#ISkillDescriptor)
  + [ISkillBinding](#ISkillBinding)
  + [ISkill](#ISkill)
+ [Classes](#Classes)
  + [SkillExecutionDeviceCPU](#SkillExecutionDeviceCPU)
  + [SkillExecutionDeviceGPU](#SkillExecutionDeviceGPU)
  + [SkillFeature](#SkillFeature)
  + [SkillFeatureTensorFloatValue](#SkillFeatureTensorFloatValue)
  + [SkillFeatureTensorIntValue](#SkillFeatureTensorIntValue)
  + [SkillFeatureTensorBooleanValue](#SkillFeatureTensorBooleanValue)
  + [SkillFeatureTensorStringValue](#SkillFeatureTensorStringValue)
  + [SkillFeatureImageValue](#SkillFeatureImageValue)
  + [SkillFeatureMapValue](#SkillFeatureMapValue)
  + [SkillVersion](#SkillVersion)
  + [SkillFeatureTensorDescriptor](#SkillFeatureTensorDescriptor)
  + [SkillFeatureImageDescriptor](#SkillFeatureImageDescriptor)
  + [SkillFeatureMapDescriptor](#SkillFeatureMapDescriptor)
  + [VisionSkillBindingHelper](#VisionSkillBindingHelper)

## Enums <a name="Enums"></a>

### SkillFeatureKind <a name="SkillFeatureKind"></a>
Type of an input/output feature to be bound for evaluation by the skill
Each of these values map to a corresponding ISkillFeatureDescriptor derivative that describes the format of the value
to be bound and processed by the skill.

| Fields      | Values
| ----------- |--------|
| Undefined   |0|
| Tensor      |1|
| Map         |2|
| Image       |3|


### SkillExecutionDeviceKind <a name="SkillExecutionDeviceKind"></a>
Type of execution device where execution of a skill can take place
This device factors into the instatiation of the skill and the memory placement of SkillFeatureValues.

| Fields      | Values
| ----------- |--------|
| Cpu         |0|
| Gpu         |1|
| Cloud       |2|


### SkillElementKind <a name="SkillElementKind"></a>
Type of element found in composite [ISkillFeatureValue](#ISkillFeatureValue)s like tensor and map.

| Fields      | Values
| ----------- |--------|
| Undefined   |0|
| Float       |1|
| Int32       |2|
| String      |3|
| Boolean     |4|


### D3DFeatureLevelKind <a name="D3DFeatureLevelKind"></a>
Feature level support of an [IDirect3DDevice][IDirect3DDevice].
Note that field values correlate with the native [D3D_FEATURE_LEVEL Enumeration][D3D_FEATURE_LEVEL].

| Fields                  | Values
| ----------------------- |--------|
| D3D_FEATURE_LEVEL_9_1   |37120|
| D3D_FEATURE_LEVEL_9_2   |37376|
| D3D_FEATURE_LEVEL_9_3   |40960|
| D3D_FEATURE_LEVEL_10_0  |41216|
| D3D_FEATURE_LEVEL_10_1  |45056|
| D3D_FEATURE_LEVEL_11_0  |45056|
| D3D_FEATURE_LEVEL_11_1  |45312|
| D3D_FEATURE_LEVEL_12_0  |49152|
| D3D_FEATURE_LEVEL_12_1  |49408|


## Interfaces<a name="Interfaces"></a>

### ISkillExecutionDevice <a name="ISkillExecutionDevice"></a>

Base interface for an execution device with which a skill can be run

#### Properties
-----

##### Name

The name of this device.

```csharp
string Name{ get; }
```
-----

##### ExecutionDeviceKind

The [SkillExecutionDeviceKind](#SkillExecutionDeviceKind) of this device.

```csharp
SkillExecutionDeviceKind ExecutionDeviceKind { get; }
```
-----


### ISkillFeature <a name="ISkillFeature"></a>

Base interface for a skill feature that encapsulates a [ISkillFeatureValue](#ISkillFeatureValue) to be indexed in a ISkillBinding

#### Properties
-----

##### Descriptor

The [ISkillFeatureDescriptor](#ISkillFeatureDescriptor) associated with this feature.

```csharp
ISkillFeatureDescriptor Descriptor{ get; }
```
-----
##### Device

The [ISkillExecutionDevice](#ISkillExecutionDevice) which dictates the memory space where this feature resides.

```csharp
ISkillExecutionDevice Device{ get; }
```
-----
##### FeatureValue

The value set on this feature.

```csharp
ISkillFeatureValue FeatureValue{ get; }
```
----- 

#### Methods
-----
##### SetFeatureValueAsync(oject)

Set a value to this feature.

```csharp
IAsyncAction SetFeatureValueAsync(object value);
```

###### Parameters
**`value`** : object

The value to be set onto this feature. 
Note that this has to be of one of the concrete value type used to instantiate a [ISkillFeatureValue](#ISkillFeatureValue) derivative.
see factory method for:
  + [SkillFeatureTensorFloatValue](#SkillFeatureTensorFloatValue)
  + [SkillFeatureTensorIntValue](#SkillFeatureTensorIntValue)
  + [SkillFeatureTensorBooleanValue](#SkillFeatureTensorBooleanValue)
  + [SkillFeatureTensorStringValue](#SkillFeatureTensorStringValue)
  + [SkillFeatureImageValue](#SkillFeatureImageValue)
  + [SkillFeatureMapValue](#SkillFeatureMapValue)


###### Returns
[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----

##### SetFeatureValueAsync(object, ISkillFeatureDescriptor)

Set a value to this feature that follows a particular descriptor.

```csharp
IAsyncAction SetFeatureValueAsync(object value, ISkillFeatureDescriptor descriptor);
```
###### Parameters
**`value`** : object

The value to be set onto this feature.
Note that this has to be of one of the concrete value type used to instantiate a [ISkillFeatureValue](#ISkillFeatureValue) derivative.
see factory method for:
  + [SkillFeatureTensorFloatValue](#SkillFeatureTensorFloatValue)
  + [SkillFeatureTensorIntValue](#SkillFeatureTensorIntValue)
  + [SkillFeatureTensorBooleanValue](#SkillFeatureTensorBooleanValue)
  + [SkillFeatureTensorStringValue](#SkillFeatureTensorStringValue)
  + [SkillFeatureImageValue](#SkillFeatureImageValue)
  + [SkillFeatureMapValue](#SkillFeatureMapValue)

**`descriptor`** : [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

the descriptor of this feature

###### Returns
[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----


### ISkillFeatureValue <a name="ISkillFeatureValue"></a>

Base interface for a feature value.

#### Properties
-----

##### Descriptor

The feature descriptor associated with this feature value.

```csharp
ISkillFeatureDescriptor Descriptor{ get; }
```
----- 


### ISkillFeatureDescriptor <a name="ISkillFeatureDescriptor"></a>

Base interface for a feature descriptor that provides information about a skill variable.

#### Properties
-----

##### Name

Skill variable name.

```csharp
string Name{ get; }
```
-----

##### Description

Skill variable description.

```csharp
string Description{ get; }
```
-----

##### IsRequired

Whether or not this feature is required to be bound in a [ISkillBinding](#ISkillBinding) to proceed to evaluation 
with the related [ISkill](#ISkill).

```csharp
bool IsRequired{ get; }
```
----- 
##### FeatureKind

The [SkillFeatureKind](#SkillFeatureKind) of an [ISkillFeature](#ISkillFeature) variable.

```csharp
SkillFeatureKind FeatureKind{ get; }
```
-----


### ISkillFeatureTensorDescriptor <a name="ISkillFeatureTensorDescriptor"></a>
`requires` [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

Provides information about a [ISkillFeatureValue](#ISkillFeatureValue) derivative of [SkillFeatureKind](#SkillFeatureKind) Tensor.

#### Properties
-----

##### ElementKind

The [SkillElementKind](#SkillElementKind) contained in this tensor feature.

```csharp
SkillElementKind ElementKind { get; }
```
-----

##### Shape

The dimensions required for this tensor feature as [IReadOnlyList][IReadOnlyList]

```csharp
IReadOnlyList <long> Shape{ get; }
```
-----


### ISkillFeatureMapDescriptor <a name="ISkillFeatureMapDescriptor"></a>
`requires` [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

Provides information about a [ISkillFeatureValue](#ISkillFeatureValue) derivative of [SkillFeatureKind](#SkillFeatureKind) Map.

#### Properties
-----

##### ValueElementKind

The [SkillElementKind](#SkillElementKind) of the values required to be held by the map.

```csharp
SkillElementKind ValueElementKind{ get; }
```
-----

##### KeyElementKind

The [SkillElementKind](#SkillElementKind) of the keys required to be held by the map.

```csharp
SkillElementKind KeyElementKind{ get; }
```
-----

##### ValidKeys

The [IIterable][IIterable] of valid keys to use as keys in the map

```csharp
IIterable<object> ValidKeys { get; }
```
-----


### ISkillFeatureImageDescriptor <a name="ISkillFeatureImageDescriptor"></a>
`requires` [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

Provides information about a [ISkillFeatureValue](#ISkillFeatureValue) derivative of [SkillFeatureKind](#SkillFeatureKind) Image.

#### Properties
-----

##### Height

The required image height (in pixels).

```csharp
int Height { get; }
```
-----

##### Width

The required image width (in pixels).

```csharp
int Width { get; }
```
-----

##### MaxDimension

The maximum image width or height allowed (in pixels).

```csharp
int MaxDimension { get; }
```
-----

##### SupportedBitmapPixelFormat

The required image [BitmapPixelFormat][BitmapPixelFormat].

```csharp
Windows.Graphics.Imaging.BitmapPixelFormat SupportedBitmapPixelFormat{ get; }
```
-----

##### SupportedBitmapAlphaMode

The required image [BitmapAlphaMode][BitmapAlphaMode].

```csharp
Windows.Graphics.Imaging.BitmapAlphaMode SupportedBitmapAlphaMode{ get; }
```
-----


### ISkillDescriptor <a name="ISkillDescriptor"></a>

Base interface for providing information about a [ISkill](#ISkill) implementation at runtime.

#### Properties
-----

##### Id

Skill [Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid?view=netcore-2.2) identifier.

```csharp
Guid Id{ get; }
```
-----

##### Name

User readable name for the skill.

```csharp
string Name{ get; }
```
-----

##### Description

User readable description for the skill.

```csharp
string Description{ get; }
```
-----

##### Version

[SkillVersion](#SkillVersion) used to differentiate between versions of the same skill contract.

```csharp
SkillVersion Version{ get; }
```
-----

##### InputFeatureDescriptors

[IReadOnlyList][IReadOnlyList] 
of [ISkillFeatureDescriptor](#ISkillFeatureDescriptor) that describe the skill inputs.
        
```csharp
IReadOnlyList <ISkillFeatureDescriptor> InputFeatureDescriptors{ get; }
```
-----

##### OutputFeatureDescriptors

[IReadOnlyList][IReadOnlyList] 
of [ISkillFeatureDescriptor](#ISkillFeatureDescriptor) that describe the skill outputs.
        
```csharp
IReadOnlyList <ISkillFeatureDescriptor> OutputFeatureDescriptors{ get; }
```
-----

##### Metadata

[IReadOnlyDictionary][IReadOnlyDictionary]
of skill metadata. This is an additional mean to convey information about the skill. 
Refer to the specific skill's documentation for details about available metadata.

```csharp
IMapView <string, string> Metadata{ get; }
```
-----

#### Methods
-----
##### GetSupportedExecutionDevicesAsync() <a name="ISkillDescriptor.GetSupportedExecutionDevicesAsync"></a>

Set a value to this feature.

```csharp
IAsyncOperation <IReadOnlyList<ISkillExecutionDevice>> GetSupportedExecutionDevicesAsync();
```

###### Returns
[IAsyncOperation][IAsyncOperation]
<[IReadOnlyList][IReadOnlyList]
<[ISkillExecutionDevice](#ISkillExecutionDevice)>>

The list of supported [ISkillExecutionDevice](#ISkillExecutionDevice) by this skill that can be used to execute its logic.
Note that each skill defines its own logic to test each available device against its skill requirements and returns only the valid ones.

-----

##### CreateSkillAsync(ISkillExecutionDevice)

Creates a skill for which it holds the descriptor.

```csharp
IAsyncOperation <ISkill> CreateSkillAsync(ISkillExecutionDevice executionDevice);
```

###### Parameters
**`executionDevice`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to instantiate the skill with that will be leveraged to execute its logic.


###### Returns
[IAsyncOperation][IAsyncOperation]
<[ISkill](#ISkill)>

The [ISkill](#ISkill) created.

-----


### ISkillBinding <a name="ISkillBinding"></a> 
`requires` [IReadOnlyDictionary][IDictionary]<string, [ISkillFeature](#ISkillFeature)>


Base interface of a skill binding object that contains [ISkillFeature](#[ISkillFeature]) that themselves point to [ISkillFeatureValue](#ISkillFeatureValue) 
to be tapped into and touched by a skill for evaluation.

#### Properties
-----

##### Device

The [ISkillExecutionDevice](#ISkillExecutionDevice) associated with this binding object.

```csharp
ISkillExecutionDevice Device{ get; }
```
-----


### ISkill <a name="ISkill"></a>

Base interface for skills to implement and extend.

#### Properties
-----

##### SkillDescriptor

The [ISkillDescriptor](#ISkillDescriptor) associated with this skill.

```csharp
ISkillDescriptor SkillDescriptor{ get; }
```
-----

#### Methods
-----
##### CreateSkillBindingAsync()

Create an ISkillBinding for binding [ISkillFeature](#ISkillFeature) for evaluation.

```csharp
IAsyncOperation<ISkillBinding> CreateSkillBindingAsync();
```

###### Returns
[IAsyncOperation][IAsyncOperation]<[ISkillBinding](#ISkillBinding)>


The [ISkillBinding](#ISkillBinding) created.

-----

##### EvaluateAsync(ISkillBinding)

Run the skill logic against a set of [ISkillFeature](#ISkillFeature) contained in a [ISkillBinding](#ISkillBinding).

```csharp
IAsyncAction EvaluateAsync(ISkillBinding binding);
```

###### Parameters
**`binding`** : [ISkillBinding](#ISkillBinding)

The execution device to instantiate the skill with that will be leveraged to execute its logic.


###### Returns
[IAsyncAction][IAsyncAction]

The [ISkill](#ISkill) created.

-----


## Classes <a name="Classes"></a>

### SkillExecutionDeviceCPU <a name="SkillExecutionDeviceCPU"></a>
``implements`` [ISkillExecutionDevice](#ISkillExecutionDevice)

Provides a CPU execution device and its information useful to infer if a skill could be run with 
it appropriately in the consumer's context. Also acts as a static factory for itself.

#### Properties
-----

##### CoreCount

The amount of processing cores available on this CPU device.

```csharp
ushort CoreCount { get; }
```
-----

#### Methods
-----
##### Create()

 Instantiates a SkillExecutionDeviceCPU and returns it as a [ISkillExecutionDevice](#ISkillExecutionDevice).

```csharp
static SkillExecutionDeviceCPU Create();
```

###### Returns
[SkillExecutionDeviceCPU](#SkillExecutionDeviceCPU)

The SkillExecutionDeviceCPU instantiated.

-----


### SkillExecutionDeviceGPU <a name="SkillExecutionDeviceGPU"></a>
``implements`` [ISkillExecutionDevice](#ISkillExecutionDevice)

Provides a GPU execution device and its information useful to infer if a skill could be run with it appropriately in the consumer's context. 
Also acts as a static factory for itself.

#### Properties
-----
##### AdapterId

The adapter ID of this GPU device.

```csharp
long AdapterId { get; }
```
-----

##### DedicatedVideoMemory

The amount of dedicated video memory of this GPU device (bytes).

```csharp
long DedicatedVideoMemory { get; }
```
-----

##### IsDefault

Tells if this GPU device is considered the default one.

```csharp
bool IsDefault { get; }
```
-----

##### HighPerformanceIndex

The ranking in terms of performance of this GPU device relative to others.

```csharp
ushort HighPerformanceIndex { get; }
```
-----

##### PowerSavingIndex

The ranking in terms of power saving of this GPU device relative to others.

```csharp
ushort PowerSavingIndex { get; }
```
-----

##### MaxSupportedFeatureLevel

The maximum supported [D3DFeatureLevelKind](#D3DFeatureLevelKind) by this GPU device.

```csharp
D3DFeatureLevelKind MaxSupportedFeatureLevel { get; }
```
-----

##### Direct3D11Device

The [IDirect3DDevice][IDirect3DDevice] associated with this GPU.

```csharp
Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice Direct3D11Device { get; };
```
-----

#### Methods
-----
##### Create(IDirect3DDevice)

 Instantiates a SkillExecutionDeviceGPU.

```csharp
static SkillExecutionDeviceGPU Create(IDirect3DDevice direct3D11Device);
```
###### Parameters
**`direct3D11Device`** : [IDirect3DDevice][IDirect3DDevice]

The [IDirect3DDevice][IDirect3DDevice] corresponding to this GPU device.

###### Returns
[SkillExecutionDeviceGPU](#SkillExecutionDeviceGPU)

The SkillExecutionDeviceGPU instantiated.

-----

##### GetAvailableGpuExecutionDevices()

Obtain all SkillExecutionDeviceGPU available on the system so that they can be filtered out appropriately given the 
skill requirements by the skill developer and exposed accordingly when calling 
[ISkillDescriptor.GetSupportedExecutionDevicesAsync()](#ISkillDescriptor.GetSupportedExecutionDevicesAsync).

```csharp
static IReadOnlyList<SkillExecutionDeviceGPU> GetAvailableGpuExecutionDevices();
```

###### Returns
[IReadOnlyList][IReadOnlyList]<[SkillExecutionDeviceGPU](#SkillExecutionDeviceGPU)>

All SkillExecutionDeviceGPU available on the system.

*``Note : This includes only hardware devices and excludes WARP``*

-----


### SkillFeature <a name="SkillFeature"></a>
``implements`` [ISkillFeature](#ISkillFeature), [IClosable][IClosable]

Encapsulates an [ISkillFeatureValue](#ISkillFeatureValue) and acts as a static factory for itself.

#### Methods
-----
##### Create(ISkillFeatureDescriptor, ISkillExecutionDevice)

Instantiates a SkillFeature.

```csharp
static SkillFeature Create(ISkillFeatureDescriptor descriptor, ISkillExecutionDevice device);
```

###### Parameters
**`descriptor`** : [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

The ISkillFeatureDescriptor associated with this feature.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device that the consuming skill is instantiated with which indicates in what memory space 
the feature should be mapped to.

###### Returns
[SkillFeature](#SkillFeature)


The [SkillFeature](#SkillFeature) created.

-----

### SkillFeatureTensorFloatValue <a name="SkillFeatureTensorFloatValue"></a>
``implements`` [ISkillFeatureValue](#ISkillFeatureValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Float.
Also acts as a static factory for itself.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <long> Shape{ get; }
```
-----

##### GetAsVectorView

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <float> GetAsVectorView();
```
-----

#### Methods
-----
##### CreateAsync(IReadOnlyList< float >, ISkillFeatureTensorDescriptor, ISkillExecutionDevice)

 Instantiates a SkillFeatureTensorFloatValue.

```csharp
static IAsyncOperation<SkillFeatureTensorFloatValue> CreateAsync(IReadOnlyList<float> value, ISkillFeatureTensorDescriptor descriptor, ISkillExecutionDevice device);
```
###### Parameters
**`value`** : [IReadOnlyList][IReadOnlyList]< float >

The float values to be set.

**`descriptor`** : [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)

The feature descriptor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillFeatureTensorFloatValue](#SkillFeatureTensorFloatValue)>

The SkillFeatureTensorFloatValue instantiated.

-----
    

### SkillFeatureTensorIntValue <a name="SkillFeatureTensorIntValue"></a>
``implements`` [ISkillFeatureValue](#ISkillFeatureValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Int32.
Also acts as a static factory for itself.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <long> Shape{ get; }
```
-----

##### GetAsVectorView

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <int> GetAsVectorView();
```
-----

#### Methods
-----
##### CreateAsync(IReadOnlyList< int >, ISkillFeatureTensorDescriptor, ISkillExecutionDevice)

 Instantiates a SkillFeatureTensorIntValue.

```csharp
static IAsyncOperation<SkillFeatureTensorIntValue> CreateAsync(IReadOnlyList<int> value, ISkillFeatureTensorDescriptor descriptor, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : [IReadOnlyList][IReadOnlyList]< int >

The int values to be set.

**`descriptor`** : [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)

The feature descriptor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillFeatureTensorIntValue](#SkillFeatureTensorIntValue)>

The SkillFeatureTensorIntValue instantiated.

-----


### SkillFeatureTensorBooleanValue <a name="SkillFeatureTensorBooleanValue"></a>
``implements`` [ISkillFeatureValue](#ISkillFeatureValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Boolean.
Also acts as a static factory for itself.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <long> Shape{ get; }
```
-----

##### GetAsVectorView

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <bool> GetAsVectorView();
```
-----

#### Methods
-----
##### CreateAsync(IReadOnlyList< bool >, ISkillFeatureTensorDescriptor, ISkillExecutionDevice)

 Instantiates a SkillFeatureTensorBooleanValue.

```csharp
static IAsyncOperation<SkillFeatureTensorBooleanValue> CreateAsync(IReadOnlyList<bool> value, ISkillFeatureTensorDescriptor descriptor, ISkillExecutionDevice device);
```
###### Parameters
**`value`** : [IReadOnlyList][IReadOnlyList]< bool >

The bool values to be set.

**`descriptor`** : [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)

The feature descriptor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillFeatureTensorBooleanValue](#SkillFeatureTensorBooleanValue)>

The SkillFeatureTensorBooleanValue instantiated.

-----


### SkillFeatureTensorStringValue <a name="SkillFeatureTensorStringValue"></a>
``implements`` [ISkillFeatureValue](#ISkillFeatureValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) String.
Also acts as a static factory for itself.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <long> Shape{ get; }
```
-----

##### GetAsVectorView

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <string> GetAsVectorView();
```
-----

#### Methods
-----
##### CreateAsync(IReadOnlyList< string >, ISkillFeatureTensorDescriptor, ISkillExecutionDevice)

 Instantiates a SkillFeatureTensorStringValue.

```csharp
static IAsyncOperation<SkillFeatureTensorStringValue> CreateAsync(IReadOnlyList<string> value, ISkillFeatureTensorDescriptor descriptor, ISkillExecutionDevice device);
```
###### Parameters
**`value`** : [IReadOnlyList][IReadOnlyList]< string >

The bool values to be set.

**`descriptor`** : [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)

The feature descriptor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillFeatureTensorStringValue](#SkillFeatureTensorStringValue)>

The SkillFeatureTensorStringValue instantiated.

-----


### SkillFeatureImageValue <a name="SkillFeatureImageValue"></a>
``implements`` [ISkillFeatureValue](#ISkillFeatureValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Image. Also acts as a static factory for itself.

#### Properties
-----
##### VideoFrame

Retrieve the [VideoFrame][VideoFrame] value.

```csharp
VideoFrame VideoFrame{ get; }
```
-----

#### Methods
-----
##### CreateAsync(VideoFrame, ISkillFeatureDescriptor, ISkillExecutionDevice)

 Instantiates a SkillFeatureImageValue.

```csharp
static IAsyncOperation<SkillFeatureTensorStringValue> CreateAsync(VideoFrame value, ISkillFeatureDescriptor descriptor, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : [VideoFrame][VideoFrame]

The bool values to be set.

**`descriptor`** : [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

The feature descriptor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillFeatureImageValue](#SkillFeatureImageValue)>

The SkillFeatureImageValue instantiated.

-----


### SkillFeatureMapValue <a name="SkillFeatureMapValue"></a>
``implements`` [ISkillFeatureValue](#ISkillFeatureValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Map. Also acts as a static factory for itself.

#### Properties
-----
##### MapView

Retrieve the [IReadOnlyDictionary][IReadOnlyDictionary]< K, V > used at creation as object.

```csharp
Object MapView{ get; }
```
-----

#### Methods
-----
##### CreateAsync(object, ISkillFeatureDescriptor, ISkillExecutionDevice)

Instantiates a SkillFeatureMapValue.

```csharp
static IAsyncOperation<SkillFeatureMapValue> CreateAsync(object value, ISkillFeatureDescriptor descriptor, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : object

The value parameter needs to be of type [IReadOnlyDictionary][IReadOnlyDictionary]< K, V > with supported K and V per the [SkillElementKind](#SkillElementKind) enum.

**`descriptor`** : [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

The feature descriptor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillFeatureMapValue](#SkillFeatureMapValue)>

The SkillFeatureMapValue instantiated.

-----


### SkillVersion <a name="SkillVersion"></a>

Describes the version of the [ISkill](#ISkill). Also acts as a static factory for itself. 

#### Properties
-----
##### Major

The major version.

```csharp
ushort Major{ get; }
```
-----

##### Minor

The minor version.

```csharp
ushort Minor{ get; }
```
-----

##### Author

The author's name.

```csharp
string Author{ get; }
```
-----

##### Publisher

The publisher's name.

```csharp
string Publisher{ get; }
```
-----

#### Methods
-----
##### Create(ushort, ushort, string, string)

Instantiates a SkillVersion.

```csharp
static SkillVersion Create(ushort major, ushort minor, string author, string publisher);
```

###### Parameters
**`major`** : ushort

The major version (normative).

**`minor`** : ushort

The minor version (normative).

**`author`** : string

The author's name (informative).

**`publisher`** : string

The publisher's name (informative).

###### Returns
[SkillVersion](#SkillVersion)

The SkillVersion instantiated.

-----


### VisionSkillBindingHelper <a name="VisionSkillBindingHelper"></a>
``implements`` [ISkillBinding](#ISkillBinding)

Unsealed runtimeclass that implements the IMap interface. It can be composed within a implementation that extend the [ISkillBinding](#ISkillBinding) interface to ease skill development.
Holds at least one [SkillFeature](#SkillFeature) that encapsulates an input [SkillFeatureImageValue](#SkillFeatureImageValue). 
The use of this class to store [SkillFeature](#SkillFeature)s ensures automatic population of appropriate [SkillFeatureValue](#SkillFeatureValue) at creation and 
conversion of image features to the prescribed format at bind time.

#### Properties
-----

##### Device

The [ISkillExecutionDevice](#ISkillExecutionDevice) associated with the ISkillFeatures contained in this container.

```csharp
ISkillExecutionDevice Device{ get; }
```
-----

#### Methods
-----
##### VisionSkillBindingHelper(ISkillDescriptor, ISkillExecutionDevice)

Constructor for VisionSkillBindingHelper.

```csharp
VisionSkillBindingHelper(ISkillDescriptor descriptor, ISkillExecutionDevice device);
```

###### Parameters
**`descriptor`** : [ISkillDescriptor](#ISkillDescriptor)

The descriptor of the [ISkill](#ISkill) this binding object is for.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device that dictates memory space location of bound values.

-----


##### SetInputImage(VideoFrame)

Calls the protected virtual method SetInputImageInternalAsync() which can be overridden

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters
**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns
[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----

##### SetInputImageInternal(VideoFrame)

Inserts a [SkillFeature](#SkillFeature) that encapsulates a [SkillFeatureImageValue](#SkillFeatureImageValue) created with the input [VideoFrame][VideoFrame]

```csharp
protected virtual Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters
**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns
[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----
    

### SkillFeatureTensorDescriptor <a name="SkillFeatureTensorDescriptor"></a>
``implements`` [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)

Describes a [ISkillFeatureTensorValue](#ISkillFeatureTensorValue) and acts as a static factory for itself.

#### Methods
-----
##### Create(string, string, bool, IReadOnlyList< long >, SkillElementKind)

Instantiates a SkillBinding.

```csharp
static SkillFeatureTensorDescriptor Create(
            string name,
            string description,
            bool isRequired,
            IReadOnlyList<long> shape, 
            SkillElementKind elementKind);
```

###### Parameters

**`name`** : string

The name of this feature.

**`description`** : string

The description of this feature.

**`isRequired`** : bool

Whether or not this feature is required to be bound in a [ISkillBinding](#ISkillBinding) to proceed to evaluation 
with the related [ISkill](#ISkill).

**`shape`** : [IReadOnlyList][IReadOnlyList]< long >

The dimensions required for this tensor feature as [IReadOnlyList][IReadOnlyList]

**`elementKind`** : [SkillElementKind](#SkillElementKind)

The [SkillElementKind](#SkillElementKind) required to be contained in this tensor feature.

###### Returns
[SkillFeatureTensorDescriptor](#SkillFeatureTensorDescriptor)

The SkillFeatureTensorDescriptor instantiated.

-----


### SkillFeatureImageDescriptor <a name="SkillFeatureImageDescriptor"></a>
``implements`` [ISkillFeatureImageDescriptor](#ISkillFeatureImageDescriptor)

Describes a [ISkillFeatureImageValue](#ISkillFeatureImageValue) and acts as a static factory for itself.

#### Methods
-----
##### Create(string, string, bool, int, int, int, BitmapPixelFormat, BitmapAlphaMode)

Instantiates a SkillBinding.

```csharp
static SkillFeatureImageDescriptor Create(
            string name,
            string description,
            bool isRequired,
            int width,
            int height,
            int maxDimension,
            BitmapPixelFormat supportedBitmapPixelFormat,
            BitmapAlphaMode supportedBitmapAlphaMode);
```

###### Parameters

**`name`** : string

The name of this feature.

**`description`** : string

The description of this feature.

**`isRequired`** : bool

Whether or not this feature is required to be bound in a [ISkillBinding](#ISkillBinding) to proceed to evaluation 
with the related [ISkill](#ISkill).

**`width`** : int

The required image width (in pixels).

**`height`** : int

The required image height (in pixels).

**`maxDimension`** : int

The maximum image width or height allowed (in pixels).

**`supportedBitmapPixelFormat`** : [BitmapPixelFormat][BitmapPixelFormat]

The required image [BitmapPixelFormat][BitmapPixelFormat].

**`supportedBitmapAlphaMode`** : [BitmapAlphaMode][BitmapAlphaMode]

The required image [BitmapAlphaMode][BitmapAlphaMode].

###### Returns
[SkillFeatureImageDescriptor](#SkillFeatureImageDescriptor)

The SkillFeatureImageDescriptor instantiated.

-----


### SkillFeatureMapDescriptor <a name="SkillFeatureMapDescriptor"></a>
``implements`` [ISkillFeatureMapDescriptor](#ISkillFeatureMapDescriptor)

Describes a [SkillFeatureMapValue](#SkillFeatureMapValue) and acts as a static factory for itself.

#### Methods
-----
##### Create(string, string, bool, SkillElementKind, SkillElementKind, IIterable< object >)

Instantiates a SkillBinding.

```csharp
static SkillFeatureMapDescriptor Create(
            string name,
            string description,
            bool isRequired,
            SkillElementKind valueElementKind,
            SkillElementKind keyElementKind,
            IIterable<object> validKeys);
```

###### Parameters

**`name`** : string

The name of this feature.

**`description`** : string

The description of this feature.

**`isRequired`** : bool

Whether or not this feature is required to be bound in a [ISkillBinding](#ISkillBinding) to proceed to evaluation 
with the related [ISkill](#ISkill).

**`valueElementKind`** : int

The [SkillElementKind](#SkillElementKind) of the values required to be held by the map.

**`keyElementKind`** : [SkillElementKind](#SkillElementKind)

The [SkillElementKind](#SkillElementKind) of the keys required to be held by the map.

**`validKeys`** : [IIterable][IIterable]< object >

The [IIterable][IIterable] of valid keys to use as keys in the map

###### Returns
[SkillFeatureMapDescriptor](#SkillFeatureMapDescriptor)

The SkillFeatureMapDescriptor instantiated.

-----




[IReadOnlyList]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2
[IReadOnlyDictionary]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2?view=netcore-2.2
[IIterable]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.collections.iiterable_t_
[IAsyncAction]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction
[IAsyncOperation]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncoperation_tresult_
[IDirect3DDevice]: https://docs.microsoft.com/en-us/uwp/api/windows.graphics.directx.direct3d11.idirect3ddevice
[D3D_FEATURE_LEVEL]: https://docs.microsoft.com/en-us/windows/desktop/api/d3dcommon/ne-d3dcommon-d3d_feature_level
[BitmapPixelFormat]: https://docs.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmappixelformat
[BitmapAlphaMode]: https://docs.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapalphamode
[IClosable]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iclosable
[VideoFrame]: https://docs.microsoft.com/en-us/uwp/api/Windows.Media.VideoFrame

###### Copyright (c) Microsoft Corporation. All rights reserved.