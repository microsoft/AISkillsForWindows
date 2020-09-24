# Microsoft.AI.Skills.Vision.ConceptTagger API documentation

+ [Classes](#Classes)
  + [ConceptTagScore](#ConceptTagScore)
  + [ConceptTaggerBinding](#ConceptTaggerBinding)
  + [ConceptTaggerDescriptor](#ConceptTaggerDescriptor)
  + [ConceptTaggerSkill](#ConceptTaggerSkill)


## Classes <a name="Classes"></a>

### ConceptTagScore <a name="ConceptTagScore"></a>

#### Properties

-----

##### Name

Returns the name of the tag.

```csharp
string Name{ get; }
```

-----

##### Score

Returns the nromalized score attributed to this tag.

```csharp
Single Score{ get; }
```

-----


### ConceptTaggerBinding <a name="ConceptTaggerBinding"></a>

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Methods

-----

##### SetInputImage(VideoFrame)

Bind an input image feature using a VideoFrame value.

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

##### GetTopXTagsAboveThreshold(Int32, Single)

Retrieve an amount of top tags scored above specified threshold for the image analyzed. It's possible to receive fewer tags than desired if there is less content identified reliably.

```csharp
Windows.Foundation.Collections.IReadOnlyList<ConceptTagScore> GetTopXTagsAboveThreshold(int desiredTopCount, float threshold)
```

###### Parameters

**`desiredTopCount`** : Int32

The amount of concept tags that scored above the specified threshold to return.

**`threshold`** : Single

The confidence score threshold to filter the returned result.

###### Returns

[IReadOnlyList][IReadOnlyList] of [ConceptTagScore](#ConceptTagScore)

At most the specified amount of concepts scored above threshold extracted from the input image after evaluation.

-----


### ConceptTaggerDescriptor <a name="ConceptTaggerDescriptor"></a>

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### ConceptTaggerDescriptor()

Constructor for ConceptTaggerDescriptor

```csharp
ConceptTaggerDescriptor()
```

-----

### ConceptTaggerSkill <a name="ConceptTaggerSkill"></a>

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----

[IReadOnlyList]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2
[IAsyncAction]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction
[IClosable]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iclosable
[VideoFrame]: https://docs.microsoft.com/en-us/uwp/api/Windows.Media.VideoFrame

###### Copyright (c) Microsoft Corporation. All rights reserved.