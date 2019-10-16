# Detect And Track Objects Sample

This sample demonstrates how to combine multiple Windows Skills (ObjectDetector and ObjectTracker) for a more powerful end to end story. The ObjectDetector is run periodically and is used to initialize ObjectTracker instances. The ObjectTracker tracks detected objects, and tracked objects are displayed as bounding boxes, labels, and historical paths/tracks. Tracker information is not preserved across ObjectDetector evaluations, i.e. all active trackers are cleared each time the detector is run.

## Build samples

- refer to the [sample guidelines](../README.md)
- make sure the Microsoft.AI.Skills.Vision.ObjectDetectorPreview, Microsoft.AI.Skills.Vision.ObjectTrackerPreview and Microsoft.AI.Skills.SkillInterfacePreview NuGet packages are installed on your app project

## Related topics

- [Microsoft.AI.Skills.SkillInterfacePreview API document](../../doc/Microsoft.AI.Skills.SkillInterfacePreview.md)
- [Microsoft.AI.Skills.Vision.ObjectDetectorPreview API document](../../doc/Microsoft.AI.Skills.Vision.ObjectDetectorPreview.md)
- [Microsoft.AI.Skills.Vision.ObjectTrackerPreview API document](../../doc/Microsoft.AI.Skills.Vision.ObjectTrackerPreview.md)

## Run the UWP sample

The app supports two types of media inputs: webcam and video file. Use the buttons at the top of the window to select the media source you would like to use. Click the "Details and options" expander to view more details about the ObjectTracker skill as well as configure options.

### Using the ObjectDetector skill

To learn more about the ObjectDetector skill, check out the [ObjectDetectorPreviewSample](../../../ObjectDetector/README.md)

### Using the ObjectTracker skill

To learn more about the ObjectTracker skill, check out the [ObjectTrackerPreview sample](../../../ObjectTracker/README.md)

### Sample app code walkthrough

The ObjectDetector and ObjectTracker are initialized separately via the `InitializeObjectDetectorAsync` and `InitializeObjectTrackerAsync` methods, respectively. The implementation of these methods are essentially the same as with any other skill.

```csharp
private async Task InitializeObjectTrackerAsync(ISkillExecutionDevice device = null)
{
    if (device != null)
    {
        m_trackerSkill = await m_trackerDescriptor.CreateSkillAsync(device) as ObjectTrackerSkill;
    }
    else
    {
        m_trackerSkill = await m_trackerDescriptor.CreateSkillAsync() as ObjectTrackerSkill;
    }
    m_trackerBindings = new List<ObjectTrackerBinding>();
    m_trackerHistories = new List<Queue<TrackerResult>>();
}
```

Skill evaluation is somewhat more interesting due to the combination of the two skills. In this sample, the flow used is:

1. Update existing trackers (`ObjectTrackerSkill.EvaluateAsync`)
2. If all existing trackers failed OR we've reached the detector evaluation interval, run object detection (`ObjectDetectorSkill.EvaluateAsync`)
    - After running object detection, clear all existing trackers and initialize new trackers from the detected objects

This logic is contained in `RunSkillsAsync`, which is invoked from by the `FrameSource_FrameAvailable` event handler. The handler uses locking to ensure only one skill evaluation happens at a time, dropping incoming frames as necessary.

> **NOTE:**
> The detector evaluation interval is implemented in such a way that the frame counter counts skill evaluations, as opposed to frames arriving from the media source (regardless of skill execution).
> As `FrameSource_FrameAvailable` drops frames as needed, this means the interval may seem to last longer than expected (i.e. 30 FPS is an unsafe assumption).

The sample app also uses several helper classes. These may be safely treated as black boxes, but a quick overview is:

- **ObjectTrackRenderer** - Used for rendering object tracker results to the `Canvas` as bounding boxes and history paths
- **IFrameSource** - Provides a common interface to wrap various media source types with
- **FrameReaderFrameSource** - Wrapper for [MediaFrameReader](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Capture.Frames.MediaFrameReader) for camera streaming
- **MediaPlayerFrameSource** - Wrapper for [MediaPlayer](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.Playback.MediaPlayer) for video file playback
