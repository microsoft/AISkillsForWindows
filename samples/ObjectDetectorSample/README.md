# Object Detector Windows Vision Skill sample

This sample will show how to use the Object Detector Vision Skill NuGet package to create an app that can detect and classify objects in a video feed

![Screenshot of object detector skill in action](./doc/sample_app.jpg)

## Related topics

- [Microsoft.AI.Skills.SkillInterfacePreview API document](../../doc/Microsoft.AI.Skills.SkillInterfacePreview.md)
- [Microsoft.AI.Skills.Vision.ObjectDetectorPreview API document](../../doc/Microsoft.AI.Skills.Vision.ObjectDetectorPreview.md)
- [Creating a custom Windows Vision Skill](../SentimentAnalyzerCustomSkill)
- [UWP Community Toolkit](https://docs.microsoft.com/en-us/windows/communitytoolkit/)
  - [Expander control (used in sample app)](https://docs.microsoft.com/en-us/windows/communitytoolkit/controls/expander)

## System requirements

**Client**: Windows 10 64-bit build 17763 or greater
> **IMPORTANT NOTE**  
> Due to the size of the model used for object detection and memory limitations in 32-bit architectures, the ObjectDetector skill is NOT supported in 32-bit (e.g. x86 and ARM32). As such, this sample will only build in x64 configuration.

## Build the sample

Open ObjectDetectorSample.csproj, and make sure the Microsoft.AI.Skills.Vision.ObjectDetectorPreview and Microsoft.AI.Skills.SkillInterfacePreview NuGet packages are installed

## Run the sample

The app supports two types of media inputs: webcam and video file. Use the buttons at the top of the window to select the media source you would like to use. Click the "Details and options" expander to view more details about the ObjectDetector skill as well as configure options such as the `ISkillExecutionDevice` to run the skill on as well as `ObjectKind` filters (more on that below).

## Using the ObjectDetector skill

As with all Vision Skills, the Object Detector skill is composed of an `ISkillDescription` (which holds general skill information), the `ISkill` instance (which is bound to a specific `ISkillExecutionDevice`), and the skill's `ISkillBinding` (which holds skill inputs, outputs, and any state information). You can instantiate your Object Detector skill as follows.

```csharp
ObjectDetectorDescriptor descriptor = new ObjectDetectorDescriptor();
ObjectDetectorSkill skill = await descriptor.CreateSkillAsync() as ObjectDetectorSkill; // If you don't specify an ISkillExecutionDevice, a default will be automatically selected
ObjectDetectorBinding binding = await skill.CreateSkillBindingAsync() as ObjectDetectorBinding;
```

The Object Detector skill does not define any additional inputs, so using the skill is as simple as:

```csharp
await binding.SetInputImageAsync(frame);  // frame is a Windows.Media.VideoFrame
await skill.EvaluateAsync(binding);
// Results are saved to binding object
```

You may manually interrogate the binding to find your output, but it's easiest to use the convenience field(s) defined. In this case, `ObjectDetectorBinding` has a `DetectedObjects` field, which is of type `IReadOnlyList<ObjectDetectorResult>`. `ObjectDetectorResult` is a struct containing an object detection's bounding `Rect` and classified `Kind`.

```csharp
IReadOnlyList<ObjectDetectorResult> detections = binding.DetectedObjects;
foreach (ObjectDetectorResult detection in detections)
{
    Windows.Foundation.Rect boundingRect = detection.Rect;
    ObjectKind objectKind = detection.Kind; // This enum is defined in the ObjectDetectorPreview namespace
    // Use results as desired
}
```

The object detector supports 80 types of objects, or `ObjectKind`s. However, sometimes users will only be interested in a select few `ObjectKind`s. This filtering can be easily performed using Linq queries:

```csharp
HashSet<ObjectKind> objectKindsOfInterest = new HashSet<ObjectKind> { ObjectKind.Person };
IReadOnlyList<ObjectDetectorResult> filteredDetections = binding.DetectedObjects.Where(
        detection => objectKindsOfInterest.Contains(detection.Kind)
    ).ToList();
```

## Sample app code walkthrough

The core skill initialization logic is in the `InitializeObjectDetectorAsync` method:

```csharp
private async Task InitializeObjectDetectorAsync(ISkillExecutionDevice device = null)
{
    if (device != null)
    {
        m_skill = await m_descriptor.CreateSkillAsync(device) as ObjectDetectorSkill;
    }
    else
    {
        m_skill = await m_descriptor.CreateSkillAsync() as ObjectDetectorSkill;
    }
    m_binding = await m_skill.CreateSkillBindingAsync() as ObjectDetectorBinding;
}
```

The method uses both overloads of `ISkillDescriptor.CreateSkillAsync` to show how they can be used: either let the skill select a default device, or specify the device you would like it to use.

The core skill evaluation logic can be found in `DetectObjectsAsync`:

```csharp
private async Task<IReadOnlyList<ObjectDetectorResult>> DetectObjectsAsync(VideoFrame frame)
{
    // Bind
    await m_binding.SetInputImageAsync(frame);

    // Evaluate
    await m_skill.EvaluateAsync(m_binding);
    var results = m_binding.DetectedObjects;

    // Filter results if requested
    if (m_objectKinds != null && m_objectKinds.Count > 0)
    {
        results = results.Where(det => m_objectKinds.Contains(det.Kind)).ToList();
    }

    return results;
}
```

Notice the `ObjectKind` filtering as discussed above.

Overall application initialization is performed in `OnNavigatedTo`, which calls the previously mentioned `InitializeObjectDetectorAsync` method as well as some other UI initialization. The `UpdateSkillUIAsync` method that is also called may appear to be non-trivial, but in fact it simply populates the fields in the "Details and options..." expander UI. It should be noted that the `ISkillDescriptor` is created directly here:

```csharp
// ...
m_descriptor = new ObjectDetectorDescriptor();
m_availableExecutionDevices = await m_descriptor.GetSupportedExecutionDevicesAsync();

await InitializeObjectDetectorAsync();
// ...
```

This is since we may call `InitializeObjectDetectorAsync` multiple times, such as whenever we switch `ISkillExecutionDevice`s. However, we only need to create the `ObjectDetectorDescriptor` once.

Most of the work is performed by the `frameSource_FrameAvailable` event handler. The handler uses locking to ensure only one skill evaluation happens at a time, dropping incoming frames as necessary. The handler launches a `Task` which performs the actual skill evaluation and result displaying and exits without `await`ing the task, relying on the locking behavior for synchronization.

The sample app also uses several helper classes. These may be safely treated as black boxes, but a quick overview is:

- **BoundingBoxRenderer** - Used for rendering object detections to the `Canvas` as bounding boxes and labels
- **IFrameSource** - Provides a common interface to wrap various media source types with
- **FrameReaderFrameSource** - Wrapper for [MediaFrameReader](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Capture.Frames.MediaFrameReader) for camera streaming
- **MediaPlayerFrameSource** - Wrapper for [MediaPlayer](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Playback.MediaPlayer) for video file playback