# WindowsVisionSkillsPreview

## Summary

Implementing and integrating efficient AI and Computer Vision (CV) solutions is a hard task for developers. The industry is moving at a fast pace and the amount of custom-tailored solutions coming out make it almost impossible for app developers to keep up easily. This proposed framework is meant to standardize the way AI and CV is put to use within a Windows application (i.e.: UWP, Desktop Win32, .Net Core 3.0) running on the edge. It aims to abstract away the complexity of AI and CV techniques by simply defining the concept of *skills* which are modular pieces of code that process input and produce output. The implementation that contains the complex details is encapsulated by an extensible WinRT API that inherits the base class present in this namespace, which leverages built-in Windows primitives which in-turn eases interop with built-in acceleration frameworks or external 3rd party ones. 

While this preview focuses on vision-oriented scenarios and primitives, this API is meant to accomodate any kind of input and output variable and a wide range of scenarios (Vision, Audio, Text, etc.).

## How To Use

For how to use the framework to author a Skill to be consumed, and creating an app to consume the skill, see the [SentimentAnalyzerCustomSkill](samples/SentimentAnalyzerCustomSkill) Sample. 

## Other Samples

- **[ObjectDetector](samples/ObjectDetector)**: detecting and classifying objects in images
- **[SkeletalDetector](samples/SkeletalDetector)**: estimating poses of people in images
- **[ConceptTagger](samples/ConceptTagger)**: obtaining classification scores of concepts in images
- **[ImageScanning](samples/ImageScanning)**: a set of skills to achieve content scanning scenarios such as the ones featured in *OfficeLens*
  - **CurvedEdgesDetector**: Seeks within an image the pixels that constitute the curved edges composing the contour of a given quad and returns their coordinates.
  - **ImageCleaner**:  Cleans and enhances an image given a specified preset.
  - **ImageRectifier**: Rectifies and crops an image to a rectangle plane given four UV coordinates.
  - **LiveQuadDetector** and **QuadDetector**: 
   Searches an image for quadrilateral shapes and returns the coordinates of their corners if found. The *LiveQuadDetector* is a stateful version of the *QuadDetector* that attempts to detect only 1 quadrangle and keeps track of the previous quad detected to be used as guide which optimizes tracking performance as new frames are bound over time. This is well suited for most scenarios operating over a stream of frames over time.
**QuadDetector** can be set to detect more than 1 quadrangle and will search the whole frame everytime unless a previous quadrangle is provided. 
  - **QuadEdgesDetector**: Searches an image for the horizontal and vertical lines defining a quadrilateral shape's contour and returns their coordinates.

-----

###### Copyright (c) Microsoft Corporation. All rights reserved.
