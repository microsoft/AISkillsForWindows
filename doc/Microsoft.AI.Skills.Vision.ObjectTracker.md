# Microsoft.AI.Skills.Vision.ObjectTracker API documentation

+ [Classes](#Classes)
  + [ObjectTrackerBinding](#ObjectTrackerBinding)
  + [ObjectTrackerDescriptor](#ObjectTrackerDescriptor)
  + [ObjectTrackerSkill](#ObjectTrackerSkill)

## Classes

### ObjectTrackerBinding

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Methods

-----

##### SetInputImage(VideoFrame)

```csharp
Windows.Foundation.IAsyncAction SetInputImageAsync(VideoFrame videoFrame)
```

###### Parameters

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image value.

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

#### SetEnableExpandingSearchAreaAsync(bool)

```csharp
Windows.Foundation.IAsyncAction SetEnableExpandingSearchAreaAsync(bool enableExpandingSearchArea)
```

###### Parameters

**`enableExpandingSearchArea`** : boolean

Flag to enable or disable expanding search area fallback evaluation on tracking failure. Enabling this may improve tracker accuracy on fast moving objects by dynamically enlarging the search region, but may substantially increase evaluation time

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----

#### Properties

-----

##### BoundingRect

The most recently located bounding rect of the tracked object regardless of success

```csharp
Windows.Foundation.Rect BoundingRect{ get; }
```

##### Succeeded

Flag indicating whether most recent evaluation succeeded, based on confidence of evaluation result

```csharp
bool Succeeded{ get; }
```

-----

### ObjectTrackerDescriptor

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### ObjectTrackerDescriptor()

Constructor for ObjectTrackerDescriptor

```csharp
ObjectTrackerDescriptor()
```

-----

### ObjectTrackerSkill

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----

#### Methods

-----

##### InitializeTrackerAsync(ISkillBinding, VideoFrame, Rect)

```csharp
Windows.Foundation.IAsyncAction InitializeTrackerAsync(
    Microsoft.AI.Skills.SkillInterface.ISkillBinding binding,
    Windows.Media.VideoFrame frame,
    Windows.Foundation.Rect rect);
```

###### Parameters

**`binding`** : [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

The binding to use for storing initialized tracker state information

**`videoFrame`** : [VideoFrame][VideoFrame]

The input image to use for tracker initialization

**`rect`** : [Rect][Rect]

The initial bounding box of the object to track

###### Returns

[IAsyncAction][IAsyncAction]

The asynchronous action for completing this operation

-----

[IReadOnlyList]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2
[IAsyncAction]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction
[IClosable]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iclosable
[VideoFrame]: https://docs.microsoft.com/en-us/uwp/api/Windows.Media.VideoFrame
[Rect]: https://docs.microsoft.com/en-us/uwp/api/Windows.Foundation.Rect

###### Copyright (c) Microsoft Corporation. All rights reserved.