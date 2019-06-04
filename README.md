# WindowsVisionSkillsPreview

## Summary

Implementing and integrating efficient AI and Computer Vision (CV) solutions is a hard task for developers. The industry is moving at a fast pace and the amount of custom-tailored 
solutions coming out make it almost impossible for app developers to keep up easily. This proposed framework is meant to standardize the way AI and CV is put to use within a WinRT application 
running on the edge. It aims to abstract away the complexity of AI and CV techniques by simply defining the concept of *skills* which are modular pieces of code that process input and 
produce output. The implementation that contains the complex details is encapsulated by an extensible WinRT API that inherits the base class present in this namespace, which leverages 
built-in Windows primitives which in-turn eases interop with built-in acceleration frameworks or external 3rd party ones. 

While this preview focuses on vision-oriented scenarios and primitives, this API is meant to accomodate any kind of input and output variable and a wide range of scenarios (Vision, Audio, Text, etc.).

## How To Use

For how to use the framework to author a Skill to be consumed, and creating an app to consume the skill, see the [SentimentAnalyzerCustomSkill](samples/SentimentAnalyzerCustomSkill) Sample. 

## Other Samples

- [ObjectDetector](samples/ObjectDetector): detecting and classifying objects
- [SkeletalDetector](samples/SkeletalDetector): estimating poses of people in images

-----

###### Copyright (c) Microsoft Corporation. All rights reserved.
