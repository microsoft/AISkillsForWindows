# Skeletal Detector Windows Vision Skill sample

This sample will show how to use the Skeletal Detector Skill NuGet package to create an app that can detect and identify individuals in a video or image, and the poses of their bodies. 

## Related topics

- [Microsoft.AI.Skills.SkillInterfacePreview API document](../../doc/Microsoft.AI.Skills.SkillInterfacePreview.md)
- [Microsoft.AI.Skills.Vision.SkeletalDetectorPreview API document](../../doc/Microsoft.AI.Skills.Vision.SkeletalDetectorPreview.md)
- [Creating a custom Windows Vision Skill](../SentimentAnalyzerCustomSkill)

## System requirements

**Client**: Windows 10 build 17763 or greater

## Build the sample

Open SkeletalDetectorSample.csproj, and make sure the Microsoft.AI.Skills.Vision.SkeletalDetectorPreview and Microsoft.AI.Skills.SkillInterfacePreview NuGet packages are installed.

## Run the sample

The app supports two types of media inputs: image file and webcam. Expand the Details and options tab at the top to switch execution devices, where your systems GPUs will appear if they support WinML inference. 

When input is a still image, you can hover over a joint (identified by filled circles) to see its label. When the input is a camera feed, the rendering of joint labels is ommitted, but you can capture an image from the feed with the button on the preview, and the output of this will have the joint labels rendered. 

## Known Issues/Limitations
- The Skeletal Detector is a very intensive process. One run on CPU could take 0.5-1s, depending on your system and how large the input image is.
- The Skeletal Detector does not filter its results, and may contain false positives or figures that may have been undesirably detected (such as those in the background, or those not in focus).