# Microsoft.AI.Skills.Vision.ObjectDetector API documentation

> **IMPORTANT NOTE**  
> Due to the size of the model used for object detection and memory limitations in 32-bit architectures, the ObjectDetector skill is NOT supported in 32-bit (e.g. x86 and ARM32)

+ [Enums](#Enums)
  + [ObjectKind](#ObjectKind)
+ [Classes](#Classes)
  + [ObjectDetectorResult](#ObjectDetectorResult)
  + [ObjectDetectorResultListDescriptor](#ObjectDetectorResultListDescriptor)
  + [ObjectDetectorResultListValue](#ObjectDetectorResultListValue)
  + [ObjectDetectorBinding](#ObjectDetectorBinding)
  + [ObjectDetectorDescriptor](#ObjectDetectorDescriptor)
  + [ObjectDetectorSkill](#ObjectDetectorSkill)

## Enums

### ObjectKind

| Fields    | Values |
| --------- | ------ |
| Undefined| -1 |
| Person| 0 |
| Bicycle| 1 |
| Car| 2 |
| Motorbike| 3 |
| Aeroplane| 4 |
| Bus| 5 |
| Train| 6 |
| Truck| 7 |
| Boat| 8 |
| TrafficLight| 9 |
| FireHydrant| 10 |
| StopSign| 11 |
| ParkingMeter| 12 |
| Bench| 13 |
| Bird| 14 |
| Cat| 15 |
| Dog| 16 |
| Horse| 17|
| Sheep| 18|
| Cow| 19|
| Elephant| 20|
| Bear| 21|
| Zebra| 22|
| Giraffe| 23|
| Backpack| 24|
| Umbrella| 25|
| Handbag| 26|
| Tie| 27|
| Suitcase| 28|
| Frisbee| 29|
| Skis| 30|
| Snowboard|  31
| SportsBall| 32 |
| Kite| 33 |
| BaseballBat| 34 |
| BaseballGlove| 35 |
| Skateboard| 36 |
| Surfboard| 37 |
| TennisRacket| 38 |
| Bottle| 39 |
| WineGlass| 40 |
| Cup| 41 |
| Fork| 42 |
| Knife| 43 |
| Spoon| 44 |
| Bowl| 45 |
| Banana| 46 |
| Apple| 47 |
| Sandwich| 48 |
| Orange| 49 |
| Broccoli| 50 |
| Carrot| 51 |
| HotDog| 52 |
| Pizza| 53 |
| Donut| 54 |
| Cake| 55 |
| Chair| 56 |
| Sofa| 57 |
| PottedPlant| 58 |
| Bed| 59 |
| DiningTable| 60 |
| Toilet| 61 |
| Tvmonitor| 62 |
| Laptop| 63 |
| Mouse| 64 |
| Remote| 65 |
| Keyboard| 66 |
| CellPhone| 67 |
| Microwave| 68 |
| Oven| 69 |
| Toaster| 70 |
| Sink| 71 |
| Refrigerator| 72 |
| Book| 73 |
| Clock| 74 |
| Vase| 75 |
| Scissors| 76 |
| TeddyBear| 77 |
| HairDryer| 78 |
| Toothbrush| 79 |

## Classes

### ObjectDetectorResult

#### Properties

-----

##### Rect

The bounding [Rect][Rect] of this detection

```csharp
Windows.Foundation.Rect Rect{ get; }
```

-----

##### Kind

The [ObjectKind](#ObjectKind) of this detection

```csharp
ObjectKind Kind{ get; }
```

-----

### ObjectDetectorResultListDescriptor

``implements`` [ISkillFeatureDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillFeatureDescriptor)

-----

#### Methods

-----

##### ObjectDetectorResultListDescriptor(string name, string description, bool isRequired)

Constructor for ObjectDetectorResultListDescriptor.

```csharp
ObjectDetectorResultListDescriptor(string name, string description, bool isRequired);
```

-----

### ObjectDetectorResultListValue

``implements`` [ISkillFeatureValue](./Microsoft.AI.Skills.SkillInterface.md#ISkillFeatureValue), [IClosable][IClosable]

-----

#### Methods

-----

##### GetAsVectorView()

Retrieve the readonly view of the list

```csharp
IReadOnlyList<ObjectDetectorResult> GetAsVectorView();
```

###### Returns

[IReadOnlyList][IReadOnlyList]<[ObjectDetectorResult](#ObjectDetectorResult)>

The object detections held in this instance.

-----

### ObjectDetectorBinding

``implements`` [ISkillBinding](./Microsoft.AI.Skills.SkillInterface.md#ISkillBinding)

-----

#### Methods

-----

##### SetInputImage(VideoFrame)

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

##### DetectedObjects

A readonly view of the results

```csharp
IReadOnlyList<ObjectDetectorResult> DetectedObjects { get; }
```

-----

### ObjectDetectorDescriptor

``implements`` [ISkillDescriptor](./Microsoft.AI.Skills.SkillInterface.md#ISkillDescriptor)

-----

#### Methods

-----

##### ObjectDetectorDescriptor()

Constructor for ObjectDetectorDescriptor

```csharp
ObjectDetectorDescriptor()
```

-----

### ObjectDetectorSkill

``implements`` [ISkill](./Microsoft.AI.Skills.SkillInterface.md#ISkill)

-----

[IReadOnlyList]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-2.2
[IAsyncAction]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction
[IClosable]: https://docs.microsoft.com/en-us/uwp/api/windows.foundation.iclosable
[VideoFrame]: https://docs.microsoft.com/en-us/uwp/api/Windows.Media.VideoFrame
[Rect]: https://docs.microsoft.com/en-us/uwp/api/Windows.Foundation.Rect

###### Copyright (c) Microsoft Corporation. All rights reserved.