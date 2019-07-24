# GalleryApp

## Summary
The Gallery App contains a set of vision skills released on WindowsVisionSkillsPreview. It demonstrates the capabilities of the new vision skills without the need to enlist in the GitHub repo.

## Getting started
Please read the [Windows Vision Skills (Preview)](https://docs.microsoft.com/en-us/windows/ai/windows-vision-skills/) page for more detailed information about Windows Vision Skills.

## Prerequisites
- Download or modify [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)
  - Under Workloads section: 
    - Universal Windows platform development
    - Desktop development with C++
    - .Net desktop development
    - .Net Core cross-platform development
  - Select the following SDK versions: 
    - 17134
    - 17763
    - 18362
- The [Windows 10 SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk) version 1809 (10.0; Build 17763)

## Gallery App configurations
### Properties setting
Right click the *GalleryApp (Universal Windows)* project and select properties:
- In *Application* section, please verify that the targeting version to:
  - Target version: Windows 10, version 1809 (10.0; Build 17763)
  - Min version: Windows 10, version 1809 (10.0; Build 17763)

### Package.appxamnifest
Please verify that *Webcam* is selected under Capabilities section.

### Load JSON file
Right click */Pages/SkillViewGlossary.json* and select properties. Verify the setting to the following:
- Build Action: Content
- Copy to Output Directory: Copy always

### Folder descriptions
- ./Assets: contains all the icons and images used by the app
- ./FrameSource: contains all the classes for acquiring different input types (image, video, and camera)
- ./Models: contains all the standard classes and interface to represent skills
- ./Pages: contains SkillViewGlossary.json, all skills’ xaml pages and their helper classes

## How to add new skill
The [SkillName]Page.xaml page will be used by the app to navigate from the MainPage.xaml thumbnail to your skill page that contains code to run your skill.
1. Add [SkillName]Page.xaml
   1. First you need to create a XAML page in the folder /Pages. \
      For example, ObjectDetectorPage.xaml is located in Pages folder. Isolate any skill specific helper class in separate .cs file   (example: BoundingBoxRender.cs for ObjectDetector).
   2. Change the header namespace in [SkillName]Page.xaml to \
      ```xaml
      <local:SkillPageBase
      xmlns:local="using:GalleryApp"
      […other codes…]
      >
      </local:SkillPageBase>
      ```
   **Data binding (inheriting from SkillPageBase class ensures consistent user experience, see part 2 for code behind xaml)**
   
   3. Add the backward navigation button to the page
      ```xaml
      <!--Backward navigation button-->
         <Button x:Name="BackButton" Grid.Row="0" Click="Back_Click" 
                 VerticalAlignment="Top" HorizontalAlignment="Left" 
          Style="{StaticResource NavigationBackButtonNormalStyle}"/>
      ```
   4. Add the following xaml components:
      ```xaml
      <TextBlock Name="UIMessageTextBlock" Text="{x:Bind UIMessageTextBlockText, 
          Mode=OneWay}" …/>
      <AppBarButton x:Name="UICameraButton" … IsEnabled="{x:Bind EnableButtons}"/>
      <AppBarButton x:Name="UIFilePickerButton" … IsEnabled="{x:Bind EnableButtons}"/>
      ```
      
2. Change code inside [SkillName]Page.xaml.cs 
   1. [SkillName]Page.xaml needs to be inherited from the SkillPageBase class and ISkillViewPage interface:
      ```csharp
      public sealed partial class [SkillName]Page : SkillPageBase, ISkillViewPage
      {…}
      ```
   2. Implement the GetSkillDescriptor() to display skill information on UI thumbnail. 
      ```csharp
      ISkillDescriptor ISkillViewPage.GetSkillDescriptor()
      {
          return new [SkillName]Descriptor();
      }
      ```
   3. Change the namespace in [SkillName]Page.xaml.cs to “GalleryApp” 
      ```csharp
      namespace GalleryApp
      {
          public sealed partial class [SkillName]Page : SkillPageBase, ISkillViewPage
          {…}
      }
      ```
3. Add NuGet package
   To install or update the needed NuGet package for to run your skill, refer the to the link [Install and manage packages in Visual Studio](https://docs.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)

4. Add skill information in SkillViewGlossary.json
   1. Update /Pages/SkillViewGlossary.json file. Select the category where you want your page to be listed. If none of the category fits, you can add a new category for your skill.
   
      The type of the xaml page is used when loading the page. Copy the class name _[SkillName]Page_ from [SkillName]Page.xaml.cs to the json file.
      
      For example, skills that processes image files are placed under the “Vision” category. Skills that processes audio files can be placed under “Audio” category.
      ```json
      [
        {
          "Name": "Vision",
          "SkillViews": [
            {
              "PageTypeStr": "ObjectDetectorPage"
            }
          ]
        }
      ]

      ```


###### Copyright (c) Microsoft Corporation. All rights reserved.
