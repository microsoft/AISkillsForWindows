# Microsoft.AI.Skills.Vision.ObjectDetectorPreview API documentation

+ [Enums](#Enums)
  + [JointLabel](#JointLabel)
+ [Classes](#Classes)
  + [Joint](#Joint)
  + [Limb](#Limb)
  + [SkeletalDetectorResult](#SkeletalDetectorResult)
  + [SkeletalDetectorResultListDescriptor](#SkeletalDetectorResultListDescriptor)
  + [SkeletalDetectorResultListValue](#SkeletalDetectorResultListValue)
  + [SkeletalDetectorBinding](#SkeletalDetectorBinding)
  + [SkeletalDetectorDescriptor](#SkeletalDetectorDescriptor)
  + [SkeletalDetectorSkill](#SkeletalDetectorSkill)

## Enums

### JointLabel

| Fields    | Values |
| --------- | ------ |
| Nose | 0 |
| Neck | 1 |
| RightShoulder | 2 |
| RightElbow | 3 |
| RightWrist | 4 |
| LeftShoulder | 5 |
| LeftElbow | 6 |
| LeftWrist | 7 |
| RightHip | 8 |
| RightKnee | 9 |
| RightAnkle | 10 |
| LeftHip | 11 |
| LeftKnee | 12 |
| LeftAnkle | 13 |
| RightEye | 14 |
| LeftEye | 15 |
| RightEar | 16 |
| LeftEar | 17 |
| NumJoints | 18 |

## Classes

### Joint

#### Properties

-----

##### Label

The type of joint

```csharp
Microsoft.AI.Skills.Vision.SkeletalDetectorPreview.JointLabel Label
```

-----

##### X

Normalized image coordinate between 0 and 1 in the horizontal direction.

-----

##### Y

Normalized iamge coordinate between 0 and 1 in the vertical direction.

-----

### Limb

#### Properties

-----

##### Joint1

The first of a pair of joints making up the limb.

-----

##### Joint2

The second of a pair of joints making up the limb.

-----

### SkeletalDetectorResult

#### Properties

-----

##### Limbs

The list of limbs belonging to one body.

```csharp
IReadOnlyList<Limb> Limbs{ get; }
```

-----

#### Methods

-----

##### SkeletalDetectorResult

Constructor for SkeletalDetectorResult

```csharp
SkeletalDetectorResult(IReadOnlyList<Limb> limbs)
```
-----

### SkeletalDetectorResultListDescriptor

``implements`` [ISkillFeatureDescriptor](./Microsoft.AI.Skills.SkillInterfacePreview.md#ISkillFeatureDescriptor)

-----

#### Methods

-----

##### SkeletalDetectorResultListDescriptor

Constructor for SkeletalDetectorResultListDescriptor.

```csharp
SkeletalDetectorResultListDescriptor(string name, string description, bool isRequired);
```

-----

### SkeletalDetectorResultListValue

``implements`` [ISkillFeatureValue](./Microsoft.AI.Skills.SkillInterfacePreview.md#ISkillFeatureValue), IClosable

-----

#### Methods

-----

###### GetAsVectorView()

Retrieve the readonly view of the list

```csharp
IReadOnlyList<SkeletalDetectorResult> GetAsVectorView();
```

###### Returns

IReadOnlyList<[SkeletalDetectorResult](#SkeletalDetectorResult)>

A list of detected bodies from the input.

-----

### SkeletalDetectorBinding

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterfacePreview.md#ISkillBinding)

-----

#### Methods

-----

##### SetInputImageAsync

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

#### Properties

-----

##### Bodies

A readonly view of the results

```csharp
IReadOnlyList<SkeletalDetectorResult> DetectedObjects { get; }
```

-----

### SkeletalDetectorDescriptor

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterfacePreview.md#ISkillDescriptor)

-----

#### Methods

-----

##### SkeletalDetectorDescriptor()

Constructor for SkeletalDetectorDescriptor

```csharp
SkeletalDetectorDescriptor()
```

-----

### SkeletalDetectorSkill

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterfacePreview.md#ISkill)

-----

## Additional References

[IReadOnlyList](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2)

[IAsyncAction](https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction)

[IClosable](https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iclosable)

[VideoFrame](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.VideoFrame)

[Rect](https://docs.microsoft.com/en-us/uwp/api/Windows.Foundation.Rect)

###### Copyright (c) Microsoft Corporation. All rights reserved.