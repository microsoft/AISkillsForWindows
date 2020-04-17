# ![ImageScanning logo](./doc/Logo.png) AI Skills for Windows

## Summary

Implementing and integrating efficient AI and Computer Vision (CV) solutions is a hard task for developers. The industry is moving at a fast pace and the amount of custom-tailored solutions coming out makes it almost impossible for app developers to keep up easily. 

This proposed framework is meant to standardize the way AI and CV is put to use within a Windows application (i.e.: UWP, Desktop Win32, .Net Core 3.0+) running on the edge. It aims to abstract away the complexity of AI techniques by simply defining the concept of *skills* which are modular pieces of code that process input(s) and produce output(s). The implementation that contains the complex details is encapsulated by extended WinRT API that inherits the base class present in the Microsoft.AI.Skills.SkillInterface namespace, which leverages built-in Windows primitives which in-turn eases interop with built-in hardware acceleration leveraged by frameworks such as *[Windows ML](https://docs.microsoft.com/en-us/windows/ai/windows-ml/)*.

While this release focuses on vision-oriented scenarios and primitives, this API is meant to accommodate any kind of input and output variable and a wide range of scenarios (Vision, Audio, Text, etc.). Any developer can extend this API set and expose their own AI skills. [See skills released by Intel](#IntelSkills)

If you are looking for the preview release samples a doc, we archived them in a branch here: *[Preview branch](https://github.com/microsoft/AISkillsForWindows/tree/Preview)*

## Creating a skill of your own

For how to use the AI Skills interface to author a new AI Skill of your own exposing your AI solution to other Windows app developers, and creating an actual app to consume this crafted skill, see this sample:

### **[Sentiment Analyzer Sample Skill](samples/SentimentAnalyzerSampleSkill)**

| ![FaceSentimentAnalyzer sample skill logo](./doc/FaceSentimentAnalyzerLogo.png) | Walking through the steps to create, in C++ or in C#, a sample AI Skill that leverages WinML and MediaFoundation as well as how to consume it in a Win32 Dektop, .Net Core 3.0+ or UWP app|
| -- | -- |

## Code samples for using AI skills [published by Microsoft on nuget.org](https://www.nuget.org/profiles/VisionSkills)

### **[Object Detector](samples/ObjectDetector)**

| ![ObjectDetector logo](./doc/ObjectDetectorLogo.png) | Detecting and classifying objects in images |
| -- | -- |

### **[Object Tracker](samples/ObjectTracker)**

| ![ObjectTracker logo](./doc/ObjectTrackerLogo.png) | Tracking objects in videos |
| -- | -- |

### **[Skeletal Detector](samples/SkeletalDetector)**

| ![SkeletalDetector logo](./doc/SkeletalDetectorLogo.png) | Estimating poses of people in images |
| -- | -- |

### **[Concept Tagger](samples/ConceptTagger)**

| ![ConceptTagger logo](./doc/ConceptTaggerLogo.png) | Obtaining classification scores of concepts in images |
| -- | -- |

### **[ImageScanning](samples/ImageScanning)**

| ![ImageScanning logo](./doc/ImageScanningLogo.png) | A set of AI skills to achieve content scanning scenarios such as the ones featured in *OfficeLens* |
| -- | -- |
| **[CurvedEdgesDetector](./samples/ImageScanning/README.md#CurvedEdgesDetectorExample)** | Seeks within an image the pixels that constitute the curved edges composing the contour of a given quad and returns their coordinates. |
| **[ImageCleaner](./samples/ImageScanning/README.md#ImageCleanerExample)** | Cleans and enhances an image given a specified preset. |
| **[ImageRectifier](./samples/ImageScanning/README.md#ImageRectifierExample)** | Rectifies and crops an image to a rectangle plane given four UV coordinates. |
| **[LiveQuadDetector](./samples/ImageScanning/README.md#QuadDetectorExample)** and **[QuadDetector](./samples/ImageScanning/README.md#QuadDetectorExample)** | Searches an image for quadrilateral shapes and returns the coordinates of their corners if found. The *LiveQuadDetector* is a stateful version of the *QuadDetector* that attempts to detect only 1 quadrangle and keeps track of the previous quad detected to be used as guide which optimizes tracking performance as new frames are bound over time. This is well suited for most scenarios operating over a stream of frames over time. *QuadDetector* can be set to detect more than 1 quadrangle and will search the whole frame everytime unless a previous quadrangle is provided. |
| **[QuadEdgesDetector](./samples/ImageScanning/README.md#QuadEdgesDetectorExample)** | Searches an image for the horizontal and vertical lines defining a quadrilateral shape's contour and returns their coordinates. |

## For samples using AI skills published by [Intel on nuget.org](https://www.nuget.org/profiles/IntelAISkills) see the [Intel-AI GitHub](https://github.com/intel/Intel-AI-Skills) and [this link](https://software.intel.com/en-us/ai/on-pc/skills) for further details <a name="IntelSkills"></a>
| AI Skill Name | Description |
| :-- | :-- |
| **[Background Blur](https://github.com/intel/Intel-AI-Skills/tree/master/Applications/BackgroundBlur)** | Segments out individuals while blurring the background image to highlight the individuals in the foreground. |
| **[Background Replacement](https://github.com/intel/Intel-AI-Skills/tree/master/Applications/BackgroundReplacement)** | Segments out individuals while replacing the background with a user-selected image. |
| **[Face Detection](https://github.com/intel/Intel-AI-Skills/tree/master/Applications/FaceDetection)** | Detects face(s) and returns face bounding box(es) and other attributes, such as eyes, mouths, or nose tips. |
| **[Intruder Detection](https://github.com/intel/Intel-AI-Skills/tree/master/Applications/IntruderDetection)** | Detects intruder by checking to see if an additional face or person is present in the video frame. |
| **[Person Detection](https://github.com/intel/Intel-AI-Skills/tree/master/Applications/PersonDetection)** | Detects person(s) and returns person bounding box(es). |
| **[Super Resolution](https://github.com/intel/Intel-AI-Skills/tree/master/Applications/SuperResolution)** | Converts a low-resolution image or video frame (320 x 240) to a high-resolution image (1280 x 960). |
| **[Super Resolution (WinML)](Applications/SuperResolutionWinML)** | Converts a low-resolution image or video frame (640 x 360) to a high-resolution image (1280 x 720). |

-----

###### Copyright (c) Microsoft Corporation. All rights reserved.
