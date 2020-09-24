// Copyright (c) Microsoft Corporation. All rights reserved.

#include <iomanip>
#include <iostream>
#include <string>
#include <winrt/Windows.Foundation.h>
#include <winrt/windows.foundation.collections.h>
#include <winrt/windows.media.h>
#include <winrt/windows.system.threading.h>
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.Graphics.Imaging.h>

#include "WindowsVersionHelper.h"
#include "winrt/Microsoft.AI.Skills.SkillInterface.h"
#include "winrt/Microsoft.AI.Skills.Vision.ConceptTagger.h"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::System::Threading;
using namespace winrt::Windows::Media;
using namespace winrt::Windows::Storage;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Graphics::Imaging;

using namespace Microsoft::AI::Skills::SkillInterface;
using namespace Microsoft::AI::Skills::Vision::ConceptTagger;

// enum to string lookup table for SkillExecutionDeviceKind
static const std::map<SkillExecutionDeviceKind, std::string> SkillExecutionDeviceKindLookup = {
    { SkillExecutionDeviceKind::Undefined, "Undefined" },
    { SkillExecutionDeviceKind::Cpu, "Cpu" },
    { SkillExecutionDeviceKind::Gpu, "Gpu" },
    { SkillExecutionDeviceKind::Vpu, "Vpu" },
    { SkillExecutionDeviceKind::Fpga, "Fpga" },
    { SkillExecutionDeviceKind::Cloud, "Cloud" }
};

//
// Load a VideoFrame from a specified image file path
//
VideoFrame LoadVideoFrameFromImageFile(hstring imageFilePath)
{
    VideoFrame resultFrame = nullptr;
    SoftwareBitmap softwareBitmap = nullptr;
    try
    {
        StorageFile file = StorageFile::GetFileFromPathAsync(imageFilePath).get();
        auto fileExtension = file.FileType();
        if (fileExtension != L".jpg" && fileExtension != L".png")
        {
            throw hresult_invalid_argument(L"This app parses only .png and .jpg image files");
        }

        IRandomAccessStream stream = file.OpenAsync(FileAccessMode::Read).get();

        // Create the decoder from the stream 
        BitmapDecoder decoder = BitmapDecoder::CreateAsync(stream).get();

        // Get the SoftwareBitmap representation of the file in BGRA8 format
        softwareBitmap = decoder.GetSoftwareBitmapAsync().get();

        // Convert to friendly format for UI display purpose
        softwareBitmap = SoftwareBitmap::Convert(softwareBitmap, BitmapPixelFormat::Bgra8, BitmapAlphaMode::Premultiplied);

        // Encapsulate the image in a VideoFrame instance
        resultFrame = VideoFrame::CreateWithSoftwareBitmap(softwareBitmap);

        stream.Close();
    }
    catch (hresult_error const& ex)
    {
        std::wcerr << "Could not load VideoFrame from file: " << imageFilePath.c_str() << std::endl;
        throw ex;
    }
    return resultFrame;
}

//
// App main loop
//
int main()
{  
    int topX = 5;
    float threshold = 0.7f;
    hstring fileName;
    try
    {
        // Check if we are running Windows 10.0.18362.x or above as required
        HRESULT hr = WindowsVersionHelper::EqualOrAboveWindows10Version(18362);
        if (FAILED(hr))
        {
            throw_hresult(hr);
        }

        // Parse arguments
        if (__argc < 2)
        {
            throw hresult_invalid_argument(L"Allowed command arguments: <file path to .jpg or .png> <optional top X concept tag count> <optional concept tag filter ranging between 0 and 1>\ni.e.: > ConceptTaggerSample_Desktop.exe test.jpg 5 0.7");
        }

        // Load image from specified file path
        fileName = winrt::to_hstring(__argv[1]);
        auto videoFrame = LoadVideoFrameFromImageFile(fileName);

        if (__argc > 2)
        {
            topX = std::stoi(__argv[2]);
        }
        if (__argc > 3)
        {
            threshold = std::stof(__argv[3]);
        }

        std::cout << "Concept Tagger C++/WinRT Non-packaged(win32) console App" << std::endl;

        // Set and run skill
        try
        {
            // Create the ConceptTagger skill descriptor
            auto skillDescriptor = ConceptTaggerDescriptor().as<ISkillDescriptor>();

            // Create instance of the skill
            auto skill = skillDescriptor.CreateSkillAsync().get();
            auto conceptTaggerSkill = skill.as<ConceptTaggerSkill>();
            std::cout << "Running Skill on : " << SkillExecutionDeviceKindLookup.at(skill.Device().ExecutionDeviceKind());
            std::wcout << L" : " << skill.Device().Name().c_str() << std::endl;
            std::wcout << L"Image file: " << fileName.c_str() << std::endl;
            std::wcout << L"TopX: " << std::to_wstring(topX) << std::endl;
            std::wcout << L"Threshold: " << std::to_wstring(threshold) << std::endl;

            // Create instance of the skill binding
            auto binding = skill.CreateSkillBindingAsync().get().as<ConceptTaggerBinding>();

            // Set the input image retrieved from file earlier
            binding.SetInputImageAsync(videoFrame).get();

            // Evaluate the binding
            skill.EvaluateAsync(binding).get();

            // Retrieve results and display time
            auto results = binding.GetTopXTagsAboveThreshold(topX, threshold);
            for (auto result : results)
            {
                std::wcout << L"\t- "  << result.Name().c_str() << L": " << winrt::to_hstring(result.Score()).c_str() << std::endl;
            }
        }
        catch (hresult_error const& ex)
        {
            std::wcerr << "Error:" << ex.message().c_str() << ":" << std::hex << ex.code().value << std::endl;
            return ex.code().value;
        }
    }
    catch (hresult_error const& ex)
    {
        std::cerr << "Error:" << std::hex << ex.code() << ":" << ex.message().c_str();
        return  ex.code().value;;
    }
    return 0;
}