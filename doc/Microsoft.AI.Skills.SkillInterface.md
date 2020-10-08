 # AI Skills for Windows Interface Documentation

 The *Microsoft.AI.Skills.SkillInterface* namespace provides a set of base interfaces to be extended by all *skills* as well as classes and helper methods for skill implementers 
 on Windows.

 ### Important concepts
 
 This API is meant to streamline the way *skills* work and are interacted with by developers. In that optic, each skill implements the following interfaces: 
 1. **[ISkillDescriptor](#ISkillDescriptor)** derivative that provides information on the skill and exposes its requirements (input, output, version, etc).
 2. **[ISkillBinding](#ISkillBinding)** derivative that serves as a dictionary and a conduit for input and output variables to be passed back and forth to the
 **[ISkill](#ISkill)**.
 It handles pre and post processing of the input/output data and simplifies their access.
 3. **[ISkill](#ISkill)** derivative that exposes the core logic of processing its input and forming its output via an **[ISkillBinding](#ISkillBinding)**.

 *Skills* are meant to leverage optimally the hardware capabilities (*CPU*, *GPU*, etc.) on each system they run on. Therefore *skills* have 
 to expose the set of [**ISkillExecutionDevice**](#ISkillExecutionDevice)s currently available and filter them into a list of compatible ones with their skill logic 
 so that the developer consuming their skill can best chose how to tap into supported hardware resources at runtime on a user system. 

 In order to ensure that input and output variables are passed in the correct format to the skill, the interface defines 
 **[ISkillFeature](#ISkillFeature)**, which is meant to encapsulate a value in a predefined format. This value is represented by a **[ISkillFeatureValue](#ISkillFeatureValue)**, 
from which this interface also defines multiple common derivatives of (tensor, image, string, etc.). For these derivatives, automatic 
 conversion to the appropriate format occurs when attempting to set a value that does not match the predefined format (i.e. when binding an image
in a different format than the one required).

# Microsoft.AI.Skills.SkillInterface API documentation

*For code see: [Microsoft.AI.Skills.SkillInterface.idl](../common/VisionSkillBase/Microsoft.AI.Skills.SkillInterface.idl)*

+ [Enums](#Enums)
  + [SkillFeatureKind](#SkillFeatureKind)
  + [SkillExecutionDeviceKind](#SkillExecutionDeviceKind)
  + [SkillElementKind](#SkillElementKind)
  + [D3DFeatureLevelKind](#D3DFeatureLevelKind)
  + [SIMDInstructionSetKind](#SIMDInstructionSetKind)
  + [ImageStretchKind](#ImageStretchKind)
  + [ImageInterpolationKind](#ImageInterpolationKind)
+ [Interfaces](#Interfaces)
  + [ID3D12CommandQueueWrapperNative](#ID3D12CommandQueueWrapperNative)
  + [ISkillExecutionDevice](#ISkillExecutionDevice)
  + [ISkillExecutionDeviceDX](#ISkillExecutionDeviceDX)
  + [ISkillFeature](#ISkillFeature)
  + [ISkillFeatureValue](#ISkillFeatureValue)
  + [ISkillFeatureTensorValue](#ISkillFeatureTensorValue)
  + [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)
  + [ISkillFeatureTensorDescriptor](#ISkillFeatureTensorDescriptor)
  + [ISkillFeatureMapDescriptor](#ISkillFeatureMapDescriptor)
  + [ISkillFeatureImageDescriptor](#ISkillFeatureImageDescriptor)
  + [ISkillDescriptor](#ISkillDescriptor)
  + [ISkillBinding](#ISkillBinding)
  + [ISkill](#ISkill)
+ [Classes](#Classes)
  + [D3D12CommandQueueWrapper](#D3D12CommandQueueWrapper)
  + [SkillInformation](#SkillInformation)
  + [SkillExecutionDeviceCPU](#SkillExecutionDeviceCPU)
  + [SkillExecutionDeviceDXHelper](#SkillExecutionDeviceDXHelper)
  + [SkillExecutionDeviceDirectX](#SkillExecutionDeviceDirectX)
  + [SkillExecutionDeviceDXCore](#SkillExecutionDeviceDXCore)
  + [SkillFeature](#SkillFeature)
  + [SkillFeatureTensorFloatValue](#SkillFeatureTensorFloatValue)
  + [SkillFeatureTensorIntValue](#SkillFeatureTensorIntValue)
  + [SkillFeatureTensorStringValue](#SkillFeatureTensorStringValue)  
  + [SkillFeatureTensorBooleanValue](#SkillFeatureTensorBooleanValue)
  + [SkillFeatureTensorInt16Value](#SkillFeatureTensorInt16Value)
  + [SkillFeatureTensorInt64Value](#SkillFeatureTensorInt64Value)
  + [SkillFeatureTensorUInt8Value](#SkillFeatureTensorUInt8Value)
  + [SkillFeatureTensorUInt16Value](#SkillFeatureTensorUInt16Value)
  + [SkillFeatureTensorUInt32Value](#SkillFeatureTensorUInt32Value)
  + [SkillFeatureTensorUInt64Value](#SkillFeatureTensorUInt64Value)
  + [SkillFeatureTensorFloat16Value](#SkillFeatureTensorFloat16Value)
  + [SkillFeatureTensorDoubleValue](#SkillFeatureTensorDoubleValue)
  + [SkillFeatureTensorCustomValue](#SkillFeatureTensorCustomValue)
  + [SkillFeatureImageValue](#SkillFeatureImageValue)
  + [SkillFeatureMapValue](#SkillFeatureMapValue)
  + [SkillFeatureTensorDescriptor](#SkillFeatureTensorDescriptor)
  + [SkillFeatureImageDescriptor](#SkillFeatureImageDescriptor)
  + [SkillFeatureMapDescriptor](#SkillFeatureMapDescriptor)
  + [VisionSkillBindingHelper](#VisionSkillBindingHelper)

## Enums <a name="Enums"></a>

### SkillFeatureKind <a name="SkillFeatureKind"></a>
Type of an input/output feature to be bound for evaluation by the skill
Each of these values map to a corresponding ISkillFeatureDescriptor derivative that describes the format of the value
to be bound and processed by the skill. This API defines [ISkillFeatureValue](#ISkillFeatureValue) derivatives associated with *Tensor*, *Map* and *Image* 
(see 
+ [SkillFeatureTensorFloatValue](#SkillFeatureTensorFloatValue)
+ [SkillFeatureTensorIntValue](#SkillFeatureTensorIntValue) 
+ [SkillFeatureTensorStringValue](#SkillFeatureTensorStringValue)
+ [SkillFeatureTensorBooleanValue](#SkillFeatureTensorBooleanValue)
+ [SkillFeatureTensorInt16Value](#SkillFeatureTensorInt16Value)
+ [SkillFeatureTensorInt64Value](#SkillFeatureTensorInt64Value)
+ [SkillFeatureTensorUInt8Value](#SkillFeatureTensorUInt8Value)
+ [SkillFeatureTensorUInt16Value](#SkillFeatureTensorUInt16Value)
+ [SkillFeatureTensorUInt32Value](#SkillFeatureTensorUInt32Value)
+ [SkillFeatureTensorUInt64Value](#SkillFeatureTensorUInt64Value)
+ [SkillFeatureTensorFloat16Value](#SkillFeatureTensorFloat16Value)
+ [SkillFeatureTensorDoubleValue](#SkillFeatureTensorDoubleValue)
+ [SkillFeatureTensorCustomValue](#SkillFeatureTensorCustomValue)
+ [SkillFeatureImageValue](#SkillFeatureImageValue)
+ [SkillFeatureMapValue](#SkillFeatureMapValue))
 
The *Undefined* value can be leveraged by skill developers to declare a custom kind of [ISkillFeatureValue](#ISkillFeatureValue) alongside a custom derivative of [ISkillFeatureDescriptor](#ISkillFeatureDescriptor) responsible for creating it.

| Fields      | Values
| ----------- |--------|
| Undefined   |0|
| Tensor      |1|
| Map         |2|
| Image       |3|
-----

### SkillExecutionDeviceKind <a name="SkillExecutionDeviceKind"></a>
Type of execution device where execution of a skill can take place
This device factors into the instantiation of the skill and the memory placement of SkillFeatureValues.

| Fields      | Values
| ----------- |--------|
| Undefined   |0|
| Cpu         |1|
| Gpu         |2|
| Vpu         |3|
| Fpga        |4|
| Cloud       |5|
| Tpu         |6|
| Npu         |7|
| Dsp         |8|
| Gna         |9|
-----

### SkillElementKind <a name="SkillElementKind"></a>
Type of element found in composite [ISkillFeatureValue](#ISkillFeatureValue)s like tensor and map.

| Fields      | Values
| ----------- |--------|
| Undefined   |0|
| Float       |1|
| Int32       |2|
| String      |3|
| Boolean     |4|
| Int16       |5|
| Int64       |6|
| UInt8       |7|
| UInt16      |8|
| UInt32      |9|
| UInt64      |10|
| Float16     |11|
| Double      |12|
-----

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
-----

### SIMDInstructionSetKind <a name="SIMDInstructionSetKind"></a>
SIMD instruction set support of a CPU [SkillExecutionDeviceCPU][SkillExecutionDeviceCPU].

| Fields      | Values
| ------------|--------|
| NEON        | 0|
| MMX         | 1|
| SSE1        | 2|
| SSE2        | 3|
| SSE3        | 4|
| SSSE3       | 5|
| SSE4_1      | 6|
| SSE4_2      | 7|
| SSE4_a      | 8|
| FMA3        | 9|
| FMA4        | 10|
| XOP         | 11|
| F16C        | 12|
| AES         | 13|
| SHA         | 14|
| ADX         | 15|
| ABM         | 16|
| BMI1        | 17|
| BMI2        | 18|
| AVX         | 19|
| AVX2        | 20|
| AVX512_F    | 21|
| AVX512_CD   | 22|
| AVX512_PF   | 23|
| AVX512_ER   | 24|
| AVX512_VL   | 25|
| AVX512_BW   | 26|
| AVX512_DQ   | 27|
| AVX512_IFMA | 28|
| AVX512_VBMI | 29|
-----

### ImageStretchKind <a name="ImageStretchKind"></a>
Type of stretch applied to an image.

| Fields        | Values | Comment |
| --------------|--------| ------- |
| None          | 0      | The source content is not resized. If the desired dimensions are bigger than the source dimension, black bars will fill the gap along its dimensions the bigger dimensions. If the desired dimensions are smaller than the source dimension, a center-crop is obtained.
| Fill          | 1      | The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
| Uniform       | 2      | The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.
| UniformToFill | 3      | The content is resized to fill the destination dimensions while it preserves its native aspect ratio. If the aspect ratio of the destination rectangle differs from the source, the source content is clipped to fit in the destination dimensions.
-----

### ImageInterpolationKind <a name="ImageInterpolationKind"></a>
Type of interpolation applied to an image

| Fields        | Values |
| --------------|--------|
| Bilinear      | 0 |
| Bicubic       | 1 |
| HighQuality   | 2 |

-----


## Interfaces<a name="Interfaces"></a>

### ID3D12CommandQueueWrapperNative <a name="ID3D12CommandQueueWrapperNative"></a>

Wraps a ID3D12CommandQueue, which can be used in native code to schedule work on the D3D12 device.

#### Methods
-----
##### GetD3D12CommandQueue([out] ID3D12CommandQueue** value);

Retrieve the wrapped [ID3D12CommandQueue][ID3D12CommandQueue] from native code.
```csharp
HRESULT GetD3D12CommandQueue([out] ID3D12CommandQueue** value);
```

###### Parameters
**`value`** : ID3D12CommandQueue**

The ID3D12CommandQueue** that gets initialized with the [ID3D12CommandQueue][ID3D12CommandQueue]. 

###### Returns
[HRESULT][HRESULT]

The error code indicating either success or a defined failure.

-----


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


### ISkillExecutionDeviceDX <a name="ISkillExecutionDeviceDX"></a>
``requires`` [ISkillExecutionDevice](#ISkillExecutionDevice)

Base interface for a DX based execution device with which a skill can be run

#### Properties
-----
##### AdapterId

The adapter ID of this DX device.

```csharp
long AdapterId { get; }
```
-----

##### DedicatedVideoMemory

The amount of dedicated video memory of this DX device (bytes).

```csharp
long DedicatedVideoMemory { get; }
```
-----

##### MaxSupportedFeatureLevel

The maximum supported [D3DFeatureLevelKind](#D3DFeatureLevelKind) by this DX device.

```csharp
D3DFeatureLevelKind MaxSupportedFeatureLevel { get; }
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
##### CustomFeatureId

Retrieve an identifier for this feature type if this interface is customized.
This is usefull to know if 2 features are compatible or need additional processing for example when
sourcing from a feature in runtime class that inherits from this interface.

```csharp
Guid CustomFeatureId{ get; }
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
Note that this has to be of one of the concrete value type used to create a [ISkillFeatureValue](#ISkillFeatureValue) derivative from the [ISkillFeatureDescriptor](#ISkillFeatureDescriptor) associated with this instance.
see methods for creating [ISkillFeatureValue](#ISkillFeatureValue):
  + [ISkillFeatureDescriptor.CreateValueAsync()](#ISkillFeatureDescriptor.CreateValueAsync)
  + [SkillFeatureTensorDescriptor.CreateValueAsync()](#SkillFeatureTensorDescriptor.CreateValueAsync)
  + [SkillFeatureImageDescriptor.CreateValueAsync()](#SkillFeatureImageDescriptor.CreateValueAsync)
  + [SkillFeatureMapDescriptor.CreateValueAsync()](#SkillFeatureMapDescriptor.CreateValueAsync)


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
see methods for creating [ISkillFeatureValue](#ISkillFeatureValue):
  + [ISkillFeatureDescriptor.CreateValueAsync()](#ISkillFeatureDescriptor.CreateValueAsync)
  + [SkillFeatureTensorDescriptor.CreateValueAsync()](#SkillFeatureTensorDescriptor.CreateValueAsync)
  + [SkillFeatureImageDescriptor.CreateValueAsync()](#SkillFeatureImageDescriptor.CreateValueAsync)
  + [SkillFeatureMapDescriptor.CreateValueAsync()](#SkillFeatureMapDescriptor.CreateValueAsync)

**`descriptor`** : [ISkillFeatureDescriptor](#ISkillFeatureDescriptor)

the descriptor of this feature

###### Returns
[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----

##### SourceFromOtherFeature(ISkillFeature)

Source this feature from another existing feature. If null is passed, then the feature sourcing is removed. 
When sourcing from another feature, this feature's value cannot be set.

```csharp
void SourceFromOtherFeature(ISkillFeature sourceFeature);
```
###### Parameters
**`sourceFeature`** : ISkillFeature

The feature to source from.
Note that source feature needs to be coherent with this feature (i.e. of the same SkillElementKind) otherwise an exception can be thrown.

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


### ISkillFeatureTensorValue <a name="ISkillFeatureTensorValue"></a>
``requires`` [ISkillFeatureValue](#ISkillFeatureValue)

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----


### ISkillFeatureDescriptor <a name="ISkillFeatureDescriptor"></a>

Provides requirements for a [ISkillFeatureValue](#ISkillFeatureValue) and a factory method for instantiating it.

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

#### Methods
-----
##### CreateValueAsync() <a name="ISkillFeatureDescriptor.CreateValueAsync"></a>

Create a [ISkillFeatureValue](#ISkillFeatureValue) according to the format this instance describes in the memory space of the specified device.

```csharp
IAsyncOperation<ISkillFeatureValue> CreateValueAsync(Object value, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : object

The value from which to create the [ISkillFeatureValue](#ISkillFeatureValue).

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[ISkillFeatureValue](#ISkillFeatureValue)>

The [ISkillFeatureValue](#ISkillFeatureValue) created from the value passed as argument.

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
IReadOnlyList <int> Shape{ get; }
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

> **Note**: Planar [BitmapPixelFormat values](https://docs.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmappixelformat) such as (Nv12, Yuy2 and P010) and odd dimension values are incompatible and will throw an exception.

#### Properties
-----

##### Height

The required image height (in pixels) to avoid resize. 
Note that a negative value indicates that there is no predetermined required value. 
A negative value below -1 indicates that the value needs to be a multiple of the value specified (i.e. -1 can be any value, -8 means the value must be a multiple of 8).

```csharp
int Height { get; }
```
-----

##### Width

The required image width (in pixels) to avoid resize. 
Note that a negative value indicates that there is no predetermined required value. 
A negative value below -1 indicates that the value needs to be a multiple of the value specified (i.e. -1 can be any value, -8 means the value must be a multiple of 8).

```csharp
int Width { get; }
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

##### Information

Skill [Information](#SkillInformation).

```csharp
SkillInformation Information{ get; }
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

##### CreateSkillAsync()

Creates a skill for which it holds the description and requirements. 
Let the skill decide of the optimal or default ISkillExecutionDevice available to use.

```csharp
IAsyncOperation <ISkill> CreateSkillAsync();
```

###### Returns
[IAsyncOperation][IAsyncOperation]
<[ISkill](#ISkill)>

The [ISkill](#ISkill) created.

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

Base interface for *skills* to implement and extend.

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

### D3D12CommandQueueWrapper <a name="D3D12CommandQueueWrapper"></a>

Provides a WinRT wrapper class to manage [D3D12CommandQueue][ID3D12CommandQueue] native object. Use as a ID3D12CommandQueueWrapperNative from C++ to access command queue.

#### Methods
-----
##### CreateLearningModelDevice()

Retrieve a [Windows.AI.MachineLearning.LearningModelDevice][LearningModelDevice] that corresponds to this wrapped [D3D12CommandQueue][ID3D12CommandQueue].

```csharp
Windows.AI.MachineLearning.LearningModelDevice CreateLearningModelDevice();;
```

###### Returns
Windows.AI.MachineLearning.LearningModelDevice

LearningModelDevice that corresponds to this D3D12CommandQueue.

-----


### SkillInformation <a name="SkillInformation"></a>

Contains all descriptive information about the skill and its origins.

#### Properties
-----

##### Author

User readable name of the author of the skill.

```csharp
string Author{ get; }
```
-----

##### Description

User readable description for the skill.

```csharp
string Description{ get; }
```
-----

##### Id

Skill [Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid?view=netcore-2.2) unique identifier.

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

##### Publisher

User readable name of the publisher of the skill.

```csharp
string Publisher{ get; }
```
-----

##### Version

[PackageVersion](#PackageVersion) used to differentiate between versions of the same skill contract.

```csharp
Windows.ApplicationModel.PackageVersion Version{ get; }
```
-----

#### Methods
-----
##### Create(String, String, Guid, Windows.ApplicationModel.PackageVersion, String, String)

Instantiates a [SkillInformation](#SkillInformation).

```csharp
SkillInformation Create(string name, string description, Guid id, Windows.ApplicationModel.PackageVersion version, string author, string publisher);
```
###### Parameters
**`name`** : String

User readable name for the skill.

**`description`** : String

User readable description for the skill.

**`id`** : Guid

Skill [Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid?view=netcore-2.2) unique identifier.

**`version`** : Windows.ApplicationModel.PackageVersion

[PackageVersion](#PackageVersion) used to differentiate between versions of the same skill contract.

**`author`** : String

User readable name of the author of the skill.

**`publisher`** : String

User readable name of the publisher of the skill.

###### Returns
[IAsyncOperation][IAsyncOperation]<[SkillInformation](#SkillInformation)>

Descriptive information about a skill and its origins.

-----


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

##### IsSIMDInstructionSetKindSupported()

 Tells if a specific [SIMDInstructionSetKind](#SIMDInstructionSetKind) is supported.

```csharp
 bool IsSIMDInstructionSetKindSupported(SIMDInstructionSetKind instructionSetKind)
```

###### Returns
bool

If the SIMD instruction specified set is supported.

-----


### SkillExecutionDeviceDirectX <a name="SkillExecutionDeviceDirectX"></a>
``implements`` [ISkillExecutionDeviceDX](#ISkillExecutionDeviceDX)

Provides a DirectX execution device and its information useful to infer if a skill could be run with it appropriately in the consumer's context. 
Also acts as a static factory for itself.

#### Properties
-----
##### IsDefault

Tells if this DirectX device is considered the default one.

```csharp
bool IsDefault { get; }
```
-----

##### HighPerformanceIndex

The ranking in terms of performance of this DirectX device relative to others.

```csharp
ushort HighPerformanceIndex { get; }
```
-----

##### PowerSavingIndex

The ranking in terms of power saving of this DirectX device relative to others.

```csharp
ushort PowerSavingIndex { get; }
```
-----

##### Direct3D11Device

The [IDirect3DDevice][IDirect3DDevice] associated with this DirectX.

```csharp
Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice Direct3D11Device { get; };
```
-----

##### IsD3D12Supported
Defines if [D3D12][CreateD3D12Device] API can be used with this adapter.

```csharp
bool IsD3D12Supported { get; };
```
-----

#### Methods
-----
##### Create(IDirect3DDevice)

 Instantiates a SkillExecutionDeviceDirectX.

```csharp
static SkillExecutionDeviceDirectX Create(IDirect3DDevice direct3D11Device);
```
###### Parameters
**`direct3D11Device`** : [IDirect3DDevice][IDirect3DDevice]

The [IDirect3DDevice][IDirect3DDevice] corresponding to this DirectX device.

###### Returns
[SkillExecutionDeviceDirectX](#SkillExecutionDeviceDirectX)

The SkillExecutionDeviceDirectX instantiated.

-----

##### GetAvailableDirectXExecutionDevices()

Obtain all SkillExecutionDeviceDirectX available on the system so that they can be filtered out appropriately given the 
skill requirements by the skill developer and exposed accordingly when calling 
[ISkillDescriptor.GetSupportedExecutionDevicesAsync()](#ISkillDescriptor.GetSupportedExecutionDevicesAsync).

```csharp
static IReadOnlyList<SkillExecutionDeviceDirectX> GetAvailableDirectXExecutionDevices();
```

###### Returns
[IReadOnlyList][IReadOnlyList]<[SkillExecutionDeviceDirectX](#SkillExecutionDeviceDirectX)>

All SkillExecutionDeviceDirectX available on the system.

*``Note : This includes only hardware devices and excludes WARP``*

-----


### SkillExecutionDeviceDXCore <a name="SkillExecutionDeviceDXCore"></a>
``implements`` [ISkillExecutionDeviceDX](#ISkillExecutionDeviceDX)

Provides a DirectX execution device based on [DXCore][DXCore] API set and its information useful to infer if a skill could be run with it appropriately in the consumer's context. 
Also acts as a static factory for itself.

#### Properties
-----
##### D3D12CommandQueue

Retrieve the associated [D3D12CommandQueueWrapper](#D3D12CommandQueueWrapper)

```csharp
D3D12CommandQueueWrapper D3D12CommandQueue{ get; }
```
-----

#### Methods
-----
##### Create(D3D12CommandQueueWrapper)

Creates a SkillExecutionDeviceDXCore instance from a D3D12CommandQueueWrapper.

```csharp
static SkillExecutionDeviceDXCore Create(D3D12CommandQueueWrapper direct3D12CommandQueueWrapper);
```
###### Parameters
**`direct3D12CommandQueueWrapper`** : [D3D12CommandQueueWrapper][D3D12CommandQueueWrapper]

The [D3D12CommandQueueWrapper][D3D12CommandQueueWrapper] corresponding to this DXCore device.

###### Returns
[SkillExecutionDeviceDXCore](#SkillExecutionDeviceDXCore)

The SkillExecutionDeviceDXCore instantiated.

-----

##### GetAvailableDXCoreExecutionDevices()

Obtain all SkillExecutionDeviceDXCore available on the system so that they can be filtered out appropriately given the 
skill requirements by the skill developer and exposed accordingly when calling 
[ISkillDescriptor.GetSupportedExecutionDevicesAsync()](#ISkillDescriptor.GetSupportedExecutionDevicesAsync).

```csharp
static IReadOnlyList<SkillExecutionDeviceDXCore> GetAvailableDXCoreExecutionDevices();
```

###### Returns
[IReadOnlyList][IReadOnlyList]<[SkillExecutionDeviceDXCore](#SkillExecutionDeviceDXCore)>

All SkillExecutionDeviceDXCore available on the system.

*``Note : This includes only hardware devices and excludes WARP``*

-----


### SkillExecutionDeviceDXHelper <a name="SkillExecutionDeviceDXHelper"></a>

Acts as a static factory to enumerate available DX based execution devices. (Prefers use of DXGI APIs if available, else uses DXCore APIs if available to enumerate GPUs. 
Always uses DXcore to enumerate non-GPU hardware.

#### Methods
-----
##### GetAvailableDXExecutionDevices()
        
Retrieve all available DirectX devices on the system. The list can contain [DXGI](#SkillExecutionDeviceDirectX) and/or [DXCore](#SkillExecutionDeviceDXCore) execution devices depending upon supported API set on the target and type of hardware (i.e. VPU, GPU, etc.)

```csharp
static Windows.Foundation.Collections.IVectorView<ISkillExecutionDeviceDX> GetAvailableDXExecutionDevices();
```

###### Returns
[ISkillExecutionDeviceDX](#ISkillExecutionDeviceDX)

The available [ISkillExecutionDeviceDX](#ISkillExecutionDeviceDX) on the system.

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
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Float.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <float> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< float >

The value contained within this instance.

-----
    

### SkillFeatureTensorIntValue <a name="SkillFeatureTensorIntValue"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Int32.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <int> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< int >

The value contained within this instance.

-----


### SkillFeatureTensorStringValue <a name="SkillFeatureTensorStringValue"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) String.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <string> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< string >

The value contained within this instance.

-----


### SkillFeatureTensorBooleanValue <a name="SkillFeatureTensorBooleanValue"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Boolean.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <bool> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< bool >

The value contained within this instance.

-----


### SkillFeatureTensorInt16Value <a name="SkillFeatureTensorInt16Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Int16.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <Int16> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< Int16 >

The value contained within this instance.

-----


### SkillFeatureTensorInt64Value <a name="SkillFeatureTensorInt64Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Int64.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <Int64> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< Int64 >

The value contained within this instance.

-----


### SkillFeatureTensorUInt8Value <a name="SkillFeatureTensorUInt8Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) UInt8.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <Byte> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< Byte >

The value contained within this instance.

-----


### SkillFeatureTensorUInt16Value <a name="SkillFeatureTensorUInt16Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) UInt16.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <UInt16> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< UInt16 >

The value contained within this instance.

-----


### SkillFeatureTensorUInt32Value <a name="SkillFeatureTensorUInt32Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) UInt32.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <UInt32> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< UInt32 >

The value contained within this instance.

-----


### SkillFeatureTensorUInt64Value <a name="SkillFeatureTensorUInt64Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) UInt64.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <UInt64> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< UInt64 >

The value contained within this instance.

-----


### SkillFeatureTensorFloat16Value <a name="SkillFeatureTensorFloat16Value"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Float16.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <Single> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< Single >

The value contained within this instance.

-----


### SkillFeatureTensorDoubleValue <a name="SkillFeatureTensorDoubleValue"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Double.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <Double> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< Double >

The value contained within this instance.

-----


### SkillFeatureTensorCustomValue <a name="SkillFeatureTensorCustomValue"></a>
``implements`` [ISkillFeatureTensorValue](#ISkillFeatureTensorValue), [IClosable][IClosable]

Defines a SkillFeatureValue of [SkillFeatureKind](#SkillFeatureKind) Tensor of [SkillElementKind](#SkillElementKind) Undefined that have to be cast to their type upon usage.

#### Properties
-----
##### Shape

Retrieve the shape of the tensor.

```csharp
IReadOnlyList <int> Shape{ get; }
```
-----

#### Methods

##### GetAsVectorView()

Retrieve the readonly view of the tensor.

```csharp
IReadOnlyList <Object> GetAsVectorView();
```

###### Returns
[IReadOnlyList][IReadOnlyList]< Object >

The value contained within this instance that can be cast to its expected type.

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

Describes a [ISkillFeatureTensorValue](#ISkillFeatureTensorValue).

#### Methods
-----
##### SkillFeatureTensorDescriptor(string, string, bool, IReadOnlyList< long >, SkillElementKind)

SkillFeatureTensorDescriptor constructor.

```csharp
SkillFeatureTensorDescriptor(
            string name,
            string description,
            bool isRequired,
            IReadOnlyList<int> shape, 
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

##### CreateValueAsync() <a name="SkillFeatureTensorDescriptor.CreateValueAsync"></a>

Create a [ISkillFeatureValue](#ISkillFeatureValue) of [SkillFeatureKind](#SkillFeatureKind) *Tensor* according to the shape specified with the specified input value argument.
Acceptable value types are IVector, IVectorView, ISkillFeatureTensorValue.

```csharp
IAsyncOperation<ISkillFeatureValue> CreateValueAsync(Object value, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : object

The value from which to create the [ISkillFeatureValue](#ISkillFeatureValue). Note that it has to be a [IReadOnlyList][IReadOnlyList]<TYPE> where TYPE is the primitive that correlates to the [SkillElementKind](#SkillElementKind)> contained in the tensor.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[ISkillFeatureValue](#ISkillFeatureValue)>

The [ISkillFeatureValue](#ISkillFeatureValue) created from the value passed as argument.

-----


### SkillFeatureImageDescriptor <a name="SkillFeatureImageDescriptor"></a>
``implements`` [ISkillFeatureImageDescriptor](#ISkillFeatureImageDescriptor)

Describes a [ISkillFeatureImageValue](#ISkillFeatureImageValue).

#### Properties
-----

##### ImageStretchKindApplied

The [ImageStretchKind](#ImageStretchKind) applied to the input value upon calling [*CreateValueAsync()*](SkillFeatureImageDescriptor.CreateValueAsync). The default value is UniformToFill

```csharp
ImageStretchKind ImageStretchKindApplied{ get;  set;}
```
-----

##### ImageInterpolationKindApplied

The [ImageInterpolationKind](#ImageInterpolationKind) applied to the input value upon scaling [*CreateValueAsync()*](SkillFeatureImageDescriptor.CreateValueAsync) if ImageStretchKindApplied is set to Fill. The default value is HighQuality

```csharp
ImageInterpolationKind ImageInterpolationKindApplied { get;  set;}
```
-----

#### Methods
-----
##### SkillFeatureImageDescriptor(string, string, bool, int, int, int, BitmapPixelFormat, BitmapAlphaMode)

SkillFeatureImageDescriptor constructor

```csharp
SkillFeatureImageDescriptor(
            string name,
            string description,
            bool isRequired,
            int width,
            int height,
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

**`supportedBitmapPixelFormat`** : [BitmapPixelFormat][BitmapPixelFormat]

The required image [BitmapPixelFormat][BitmapPixelFormat].

**`supportedBitmapAlphaMode`** : [BitmapAlphaMode][BitmapAlphaMode]

The required image [BitmapAlphaMode][BitmapAlphaMode].

###### Returns
[SkillFeatureImageDescriptor](#SkillFeatureImageDescriptor)

The SkillFeatureImageDescriptor instantiated.

-----

##### SkillFeatureImageDescriptor(string, string, bool, int, int, int, BitmapPixelFormat, BitmapAlphaMode)

SkillFeatureImageDescriptor constructor

```csharp
SkillFeatureImageDescriptor(
            string name,
            string description,
            bool isRequired,
            int width,
            int height,
            BitmapPixelFormat supportedBitmapPixelFormat,
            BitmapAlphaMode supportedBitmapAlphaMode,
            ImageStretchKind imageStretchKindApplied,
            ImageInterpolationKind imageInterpolationKind);
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

**`supportedBitmapPixelFormat`** : [BitmapPixelFormat][BitmapPixelFormat]

The required image [BitmapPixelFormat][BitmapPixelFormat].

**`supportedBitmapAlphaMode`** : [BitmapAlphaMode][BitmapAlphaMode]

The required image [BitmapAlphaMode][BitmapAlphaMode].

**`imageStretchKindApplied`** : [ImageStretchKind](#ImageStretchKind)

The image stretch applied.

**`imageInterpolationKindApplied`** : [imageInterpolationKind](#imageInterpolationKind)

The image interpolation leveraged when using a *Fill* [ImageStretchKind](#ImageStretchKind).

###### Returns
[SkillFeatureImageDescriptor](#SkillFeatureImageDescriptor)

The SkillFeatureImageDescriptor instantiated.

-----

##### CreateValueAsync() <a name="SkillFeatureImageDescriptor.CreateValueAsync"></a>

Create a [ISkillFeatureValue](#ISkillFeatureValue) of [SkillFeatureKind](#SkillFeatureKind) *Image* according to the format specified.
Acceptable value types are [VideoFrame][VideoFrame], ISkillFeatureImageValue.

```csharp
IAsyncOperation<ISkillFeatureValue> CreateValueAsync(Object value, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : object

The value from which to create the [ISkillFeatureValue](#ISkillFeatureValue). Note that it has to be a [VideoFrame][VideoFrame].

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[ISkillFeatureValue](#ISkillFeatureValue)>

The [ISkillFeatureValue](#ISkillFeatureValue) created from the value passed as argument.

---


### SkillFeatureMapDescriptor <a name="SkillFeatureMapDescriptor"></a>
``implements`` [ISkillFeatureMapDescriptor](#ISkillFeatureMapDescriptor)

Describes a [SkillFeatureMapValue](#SkillFeatureMapValue).

#### Methods
-----
##### SkillFeatureMapDescriptor(string, string, bool, SkillElementKind, SkillElementKind, IIterable< object >)

SkillFeatureMapDescriptor constructor.

```csharp
SkillFeatureMapDescriptor(
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

##### CreateValueAsync() <a name="SkillFeatureMapDescriptor.CreateValueAsync"></a>

Create a [ISkillFeatureValue](#ISkillFeatureValue) of [SkillFeatureKind](#SkillFeatureKind) *Map* according to the key and value [SkillElementKind](#SkillElementKind)s specified.
Acceptable value types are IMapView, IMap, ISkillFeatureMapValue.
```csharp
IAsyncOperation<ISkillFeatureValue> CreateValueAsync(Object value, ISkillExecutionDevice device);
```

###### Parameters
**`value`** : object

The value from which to create the [ISkillFeatureValue](#ISkillFeatureValue). Note that it has to be a [IReadOnlyDictionary][IReadOnlyDictionary]<TYPE1, TYPE2> where TYPE1 and TYPE2 are the primitives that correlate to the [SkillElementKind](#SkillElementKind)> contained in the map.

**`device`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to be used by the skill which dictates the memory space.

###### Returns
[IAsyncOperation][IAsyncOperation]<[ISkillFeatureValue](#ISkillFeatureValue)>

The [ISkillFeatureValue](#ISkillFeatureValue) created from the value passed as argument.

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
[ID3D12CommandQueue]: https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nn-d3d12-id3d12commandqueue
[CreateD3D12Device]: https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-d3d12createdevice
[LearningModelDevice]: https://docs.microsoft.com/en-us/uwp/api/windows.ai.machinelearning.learningmodeldevice
[HRESULT]: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/0642cb2f-2075-4469-918c-4441e69c548a
[DXCore][https://docs.microsoft.com/en-us/windows/win32/dxcore/dxcore]

###### Copyright (c) Microsoft Corporation. All rights reserved.
