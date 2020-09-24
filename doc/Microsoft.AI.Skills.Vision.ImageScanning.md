# Microsoft.AI.Skills.Vision.ConceptTagger API documentation

+ [Enums](#Enum)
  + [ImageCleaningKind](#ImageCleaningKind)
+ [Classes](#Classes)
  + [CurvedEdgeDetector](#CurvedEdgeDetector)CurvedEdgeDetector
    + [CurvedEdgesDetectorBinding](#CurvedEdgesDetectorBinding)
    + [CurvedEdgesDetectorDescriptor](#CurvedEdgesDetectorDescriptor)
    + [CurvedEdgesDetectorSkill](#CurvedEdgesDetectorSkill)
  + [ImageCleaner](#ImageCleaner)
    + [ImageCleanerBinding](#ImageCleanerBinding)
    + [ImageCleanerDescriptor](#ImageCleanerDescriptor)
    + [ImageCleanerSkill](#ImageCleanerSkill)
  + [ImageRectifier](#ImageRectifier)
    + [ImageRectifierBinding](#ImageRectifierBinding)
    + [ImageRectifierDescriptor](#ImageRectifierDescriptor)
    + [ImageRectifierSkill](#ImageRectifierSkill)
  + [LiveQuadDetector](#LiveQuadDetector)
    + [LiveQuadDetectorBinding](#LiveQuadDetectorBinding)
    + [LiveQuadDetectorDescriptor](#LiveQuadDetectorDescriptor)
    + [LiveQuadDetectorSkill](#LiveQuadDetectorSkill)
  + [QuadDetector](#QuadDetector)
    + [QuadDetectorBinding](#QuadDetectorBinding)
    + [QuadDetectorDescriptor](#QuadDetectorDescriptor)
    + [QuadDetectorSkill](#QuadDetectorSkill)
  + [QuadEdgesDetector](#QuadEdgesDetector)
    + [QuadEdgesDetectorBinding](#QuadEdgesDetectorBinding)
    + [QuadEdgesDetectorDescriptor](#QuadEdgesDetectorDescriptor)
    + [QuadEdgesDetectorSkill](#QuadEdgesDetectorSkill)
-----

## Enums <a name="Enums"></a>

### ImageCleaningKind <a name="ImageCleaningKind"></a>
Kinds of image interpolation that can be used when rectifying an image using the  that can be applied by the [ImageCleanerSkill](#ImageCleanerSkill) and specified by binding the associated enum value to a [ImageCleanerBinding](#ImageCleanerBinding) instance. 
 
| Fields               | Values
| -------------------- |--------|
| WhiteboardOrDocument |0|
| Whiteboard           |1|
| Document             |2|
| Picture              |3|
-----


## Classes <a name="Classes"></a>

---
## **CurvedEdgeDetector** <a name="CurvedEdgeDetector"></a>
---

### CurvedEdgesDetectorBinding <a name="CurvedEdgesDetectorBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Properties

-----

##### DetectedCurvedEdges

Get the result of executing the skill against bound inputs: a set of points in normalized coordinates [0,1] defining a polyline that latches to the contour of object(s) close to the guiding bounds provided by the input base quadrangle.

```csharp
Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> DetectedCurvedEdges{ get; }
```

-----

#### Methods

-----

##### SetInputImageAsync(VideoFrame)

Bind an input image feature using a VideoFrame value.

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----


### CurvedEdgesDetectorDescriptor <a name="CurvedEdgesDetectorDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### CurvedEdgesDetectorDescriptor()

Constructor for CurvedEdgesDetectorDescriptor.

```csharp
CurvedEdgesDetectorDescriptor()
```

-----

### CurvedEdgesDetectorSkill <a name="CurvedEdgesDetectorSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----


## **ImageCleaner** <a name="ImageCleaner"></a>
---

### ImageCleanerBinding <a name="ImageCleanerBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Properties

-----

##### OutputImage

Get the result of executing the skill against bound inputs: the resulting image from processing the input image with a specified [ImageCleaningKind](#ImageCleaningKind).

```csharp
VideoFrame OutputImage{ get; }
```

-----

#### Methods

-----

##### SetInputImageAsync(VideoFrame)

Bind an input image feature using a VideoFrame value.

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

##### SetImageCleaningKindAsync(ImageCleaningKind)

Specify an [ImageCleaningKind](#ImageCleaningKind) to process the bound input image upon skill evaluation.

```csharp
Windows::Foundation::IAsyncAction SetImageCleaningKindAsync(ImageCleaningKind imageCleaningKind)
```

###### Parameters

**`imageCleaningKind`** : [ImageCleaningKind](#ImageCleaningKind)

The image cleaning kind to use.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----


### ImageCleanerDescriptor <a name="ImageCleanerDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### ImageCleanerDescriptor()

Constructor for ImageCleanerDescriptor.

```csharp
ImageCleanerDescriptor()
```

-----

### ImageCleanerSkill <a name="ImageCleanerSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----


## **ImageRectifier** <a name="ImageRectifier"></a>
---
### ImageRectifierBinding <a name="ImageRectifierBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Properties

-----

##### OutputImage

Get the result of executing the skill against bound inputs: the resulting image from cropping and rectifying the content of the image within the input bounds onto a rectangle plane using the [ImageInterpolationKind](./Microsoft.AI.Skills.SkillInterface.md#ImageInterpolationKind) specified.

```csharp
VideoFrame OutputImage{ get; }
```

-----

#### Methods

-----

##### SetInputImageAsync(VideoFrame)

Bind an input image feature using a VideoFrame value.

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

##### SetInputQuadAsync(Windows.Foundation.Collections.IVectorView<Windows.Foundation.Point>)

Specify the corners of a quadrangle in normalized coordinates [0,1] that define the bounds within the input image from which to crop and rectify content onto a rectangle plane.

```csharp
Windows::Foundation::IAsyncAction SetInputQuadAsync(Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> inputQuad)
```

###### Parameters

**`inputQuad`** : [IReadOnlyList][IReadOnlyList] of [Point][Point]s

The corners of a quadrangle in normalized coordinates [0,1].

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

##### SetInterpolationKind(ImageInterpolationKind)

Specify an [ImageInterpolationKind](./Microsoft.AI.Skills.SkillInterface.md#ImageInterpolationKind) to rectify the bound input image upon skill evaluation.

```csharp
Windows::Foundation::IAsyncAction SetInterpolationKind(ImageInterpolationKind interpolationKind)
```

###### Parameters

**`interpolationKind`** : [ImageInterpolationKind](./Microsoft.AI.Skills.SkillInterface.md#ImageInterpolationKind)

The image interpolation kind to use.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

### ImageRectifierDescriptor <a name="ImageRectifierDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### ImageRectifierDescriptor()

Constructor for ImageRectifierDescriptor.

```csharp
ImageRectifierDescriptor()
```

-----

### ImageRectifierSkill <a name="ImageRectifierSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----

## **LiveQuadDetector** <a name="LiveQuadDetector"></a>
---
### LiveQuadDetectorBinding <a name="LiveQuadDetectorBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Methods

-----

##### SetInputImageAsync(VideoFrame)

Bind an input image feature using a VideoFrame value.

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

##### DetectedQuad(out Boolean)

Get the result of executing the skill against bound inputs: the detected quadrangle corners in normalized coordinates [0,1] as well as an output similarity cue parameter stating if this quadrangle is similar to the one previously detected last time the skill was executed.

```csharp
Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> DetectedQuad(out bool isSimilarToLastQuad)
```

###### Parameters

**`isSimilarToLastQuad`** : bool

Similarity cue parameter stating if this quadrangle is similar to the one previously detected last time the skill was executed.

###### Returns

[IAsyncAction][IAsyncAction]

The corners of a quadrangle in normalized coordinates [0,1].

-----

### LiveQuadDetectorDescriptor <a name="LiveQuadDetectorDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### LiveQuadDetectorDescriptor()

Constructor for LiveQuadDetectorDescriptor.

```csharp
LiveQuadDetectorDescriptor()
```

-----

##### CreateSkillAsync(ISkillExecutionDevice, Single)

Creates a skill for which it holds the descriptor.

```csharp
IAsyncOperation <ISkill> CreateSkillAsync(ISkillExecutionDevice executionDevice, float frameRadiusLimit);
```

###### Parameters
**`executionDevice`** : [ISkillExecutionDevice](#ISkillExecutionDevice)

The execution device to instantiate the skill with that will be leveraged to execute its logic.

**`frameRadiusLimit`** : float

The percentage of the input image maximum dimension to use as a threshold to derive similarity between 2 detected quandrangles in 2 subsequent skill execution.

Default value is 10%.

###### Returns
[IAsyncOperation][IAsyncOperation]
<[ISkill](#ISkill)>

The [ISkill](#ISkill) created.

-----

### LiveQuadDetectorSkill <a name="LiveQuadDetectorSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----

## **QuadDetector** <a name="QuadDetector"></a>
---
### QuadDetectorBinding <a name="QuadDetectorBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Properties

-----

##### DetectedQuads

Get the result of executing the skill against bound inputs: the set of detected quadrangles corners in normalized coordinates [0,1] (4 first corners are for the first quadrangle, next set of 4 are for the second one, etc.).

```csharp
Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> DetectedQuads { get; }
```

-----

#### Methods

-----

##### SetInputImageAsync(VideoFrame)

Bind an input image feature using a VideoFrame value.

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

##### SetMaxQuadCount(Int32)

Set the maximum count of quads to attempt to find in the input image.

This is optional and the default value is 1.

```csharp
void SetMaxQuadCount(int maxQuadCount)
```

###### Parameters

**`maxQuadCount`** : int

The amount of quads.

-----

##### SetLookupRegionCenterCropPercentage(Int32)

Set a center crop percentage of the input image dimension to narrow down the search for quads to a sub part of the image.

This is optional and the default value is 5%.

```csharp
void SetLookupRegionCenterCropPercentage(int cropPercentage)
```

###### Parameters

**`cropPercentage`** : int

The crop percentage given the input image dimension.

-----

##### SetPreviousQuad(Windows.Foundation.Collections.IVectorView<Windows.Foundation.Point>)

Set the normalized coordinates [0,1] of the corners of a quad to guide search for the next evaluation.

This is optional and if not specified, each skill execution searches the whole image for a new set of quadrangles.

```csharp
void SetPreviousQuad(Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> previousQuad)
```

###### Parameters

**`previousQuad`** : [IReadOnlyList][IReadOnlyList] of [Point][Point]s

The normalized coordinates [0,1] of the corners of a quadrangle.

-----

##### SetCenterPoint(Windows.Foundation.IReference<Windows.Foundation.Point>)

Set the center point in normalized coordinates [0,1] of the image from where the search for quad begins.

This is optional, and by default the middle of the image is considered the center point.

```csharp
void SetCenterPoint(Windows.Foundation.Point? centerPoint)
```

###### Parameters

**`centerPoint`** : [IReference][IReference] to [Point][Point]s

The normalized coordinates [0,1] of the corners of a quadrangle.

-----

### QuadDetectorDescriptor <a name="QuadDetectorDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### QuadDetectorDescriptor()

Constructor for QuadDetectorDescriptor.

```csharp
QuadDetectorDescriptor()
```

-----

### QuadDetectorSkill <a name="QuadDetectorSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----

## **QuadEdgesDetector** <a name="QuadEdgesDetector"></a>
---
### QuadEdgesDetectorBinding <a name="QuadEdgesDetectorBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Properties

-----

##### DetectedHorizontalEdges

Get the result of executing the skill against bound inputs: the set of potential horizontal quadrangle edge points in normalized coordinates [0,1] in sets of 2 (2 first points are for the first edge, next set of 2 is for the second one, etc.).

```csharp
Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> DetectedHorizontalEdges { get; }
```

-----

##### DetectedVerticalEdges

Get the result of executing the skill against bound inputs: the set of potential vertical quadrangle edge points in normalized coordinates [0,1] in sets of 2 (2 first points are for the first edge, next set of 2 is for the second one, etc.).

```csharp
Windows.Foundation.Collections.IReadOnlyList<Windows.Foundation.Point> DetectedVerticalEdges { get; }
```

-----

#### Methods

-----

##### SetInputImageAsync(VideoFrame)

Bind an input image feature using a VideoFrame value.

```csharp
Windows::Foundation::IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation.

-----

##### SetMaxDetectedEdgeCount(Int32)

Set the maximum count of edges (both vertical and horizontal) to attempt to find in the input image.

This is optional and the default value is 10.

```csharp
void SetMaxDetectedEdgeCount(int maxEdgeCount)
```

###### Parameters

**`maxEdgeCount`** : int

The amount of quad edges.

-----

### QuadEdgesDetectorDescriptor <a name="QuadEdgesDetectorDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### QuadEdgesDetectorDescriptor()

Constructor for QuadEdgesDetectorDescriptor.

```csharp
QuadEdgesDetectorDescriptor()
```

-----

### QuadEdgesDetectorSkill <a name="QuadEdgesDetectorSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----




[IReadOnlyList]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2
[IAsyncAction]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction
[IClosable]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iclosable
[VideoFrame]: https://docs.microsoft.com/en-us/uwp/api/Windows.Media.VideoFrame
[Point]: (https://docs.microsoft.com/en-us/uwp/api/Windows.Foundation.Point)
[IReference]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.ireference_t_

###### Copyright (c) Microsoft Corporation. All rights reserved.