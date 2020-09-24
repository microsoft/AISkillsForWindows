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
#include "winrt/Microsoft.AI.Skills.Vision.ImageScanning.h"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::System::Threading;
using namespace winrt::Windows::Media;
using namespace winrt::Windows::Storage;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Graphics::Imaging;

using namespace Microsoft::AI::Skills::SkillInterface;
using namespace Microsoft::AI::Skills::Vision::ImageScanning;

// enum to string lookup table for SkillExecutionDeviceKind
static const std::map<SkillExecutionDeviceKind, std::string> SkillExecutionDeviceKindLookup = {
    { SkillExecutionDeviceKind::Undefined, "Undefined" },
    { SkillExecutionDeviceKind::Cpu, "Cpu" },
    { SkillExecutionDeviceKind::Gpu, "Gpu" },
    { SkillExecutionDeviceKind::Vpu, "Vpu" },
    { SkillExecutionDeviceKind::Fpga, "Fpga" },
    { SkillExecutionDeviceKind::Cloud, "Cloud" }
};

// enum to string lookup table for ImageInterpolationKind
static const std::map<ImageInterpolationKind, std::string> ImageInterpolationKindLookup = {
    { ImageInterpolationKind::Bilinear, "Bilinear" },
    { ImageInterpolationKind::Bicubic, "Bicubic" },
    { ImageInterpolationKind::HighQuality, "HighQuality" }
};

// enum to string lookup table for ImageCleaningKind
static const std::map<ImageCleaningKind, std::string> ImageCleaningKindLookup = {
    { ImageCleaningKind::WhiteboardOrDocument, "WhiteboardOrDocument" },
    { ImageCleaningKind::Whiteboard, "Whiteboard" },
    { ImageCleaningKind::Document, "Document" },
    { ImageCleaningKind::Picture, "Picture" },
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
// Save a modified VideoFrame using an existing image file path with an appended suffix
//
hstring SaveModifiedVideoFrameToFile(hstring imageFilePath, VideoFrame frame)
{
    try
    {
        StorageFile file = StorageFile::GetFileFromPathAsync(imageFilePath).get();
        StorageFolder folder = file.GetParentAsync().get();
        std::wstring fileNameTemp = file.Name().c_str();
        auto insertPosition = fileNameTemp.find(file.FileType());
        fileNameTemp.insert(insertPosition, L"_mod");

        StorageFile modifiedFile = folder.CreateFileAsync(fileNameTemp, CreationCollisionOption::GenerateUniqueName).get();
        imageFilePath = modifiedFile.Path();

        // Create the encoder from the stream
        IRandomAccessStream stream = modifiedFile.OpenAsync(FileAccessMode::ReadWrite).get();

        BitmapEncoder encoder = BitmapEncoder::CreateAsync(BitmapEncoder::JpegEncoderId(), stream).get();
        SoftwareBitmap softwareBitmap = frame.SoftwareBitmap();
        encoder.SetSoftwareBitmap(softwareBitmap);
        encoder.FlushAsync().get();
    }
    catch (hresult_error const& ex)
    {
        std::wcerr << L"Could not save modified VideoFrame from file: " << imageFilePath.c_str();
        throw ex;
    }
    return imageFilePath;
}

//
// App main loop
//
int main()
{
    hstring fileName;
    ImageInterpolationKind imageInterpolationKind = ImageInterpolationKind::Bilinear; // default value if none specified as argument
    ImageCleaningKind imageCleaningPreset = ImageCleaningKind::WhiteboardOrDocument; // default value if none specified as argument

    std::cout << "Image Scanning C++/WinRT Non-packaged(win32) Console App - "
        << "This app executes a common productivity scenario that consists of scanning an "
        << "input image for a quadrangle, rectifying and cropping using its corner coordinates, "
        << "cleaning its content and saving the result image to file:\n"
        << "1. finds the predominant quadrangle\n"
        << "2. uses this quadrangle to rectify and crop the image\n"
        << "3. cleans the rectified image\n\n" << std::endl;

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
            std::string errorMessage = "Allowed command arguments: <file path to .jpg or .png>";
            errorMessage = errorMessage
                + " <optional image rectifier interpolation to apply to the rectified image:\n"
                + "\t1. " + ImageInterpolationKindLookup.at(ImageInterpolationKind::Bilinear) + "\n"
                + "\t2. " + ImageInterpolationKindLookup.at(ImageInterpolationKind::Bicubic) + "\n"
                + "\t3. " + ImageInterpolationKindLookup.at(ImageInterpolationKind::HighQuality) + "\n"
                + "<optional image cleaning preset to apply to the rectified image:\n"
                + "\t1. " + ImageCleaningKindLookup.at(ImageCleaningKind::WhiteboardOrDocument) + "\n"
                + "\t2. " + ImageCleaningKindLookup.at(ImageCleaningKind::Whiteboard) + "\n"
                + "\t3. " + ImageCleaningKindLookup.at(ImageCleaningKind::Document) + "\n"
                + "\t4. " + ImageCleaningKindLookup.at(ImageCleaningKind::Picture) + "\n"
                + "i.e.: \n> ImageScanningSample_Desktop.exe test.jpg 1 1\n\n";
            throw hresult_invalid_argument(winrt::to_hstring(errorMessage));
        }

        // Load image from specified file path
        fileName = winrt::to_hstring(__argv[1]);
        auto videoFrame = LoadVideoFrameFromImageFile(fileName);

        // Parse optional image interpolation preset argument
        int selection = 0;
        if (__argc < 3)
        {
            while (selection < 1 || selection > 2)
            {
                std::cout << "Select the image rectifier interpolation to apply to the rectified image:\n"
                    << "\t1. Bilinear\n"
                    << "\t2. Bicubic\n";
                std::cin >> selection;
            }
        }
        else
        {
            selection = std::stoi(__argv[2]);
            if (selection < 1 || selection > 2)
            {
                std::cout << "Invalid image rectifier interpolation specified, defaulting to Bilinear";
                selection = (int)ImageInterpolationKind::Bilinear + 1;
            }
        }
        imageInterpolationKind = (ImageInterpolationKind)(selection - 1);

        // Parse optional image cleaning preset argument
        selection = 0;
        if (__argc < 4)
        {
            while (selection < 1 || selection > 4)
            {
                std::cout << "Select the image cleaning preset to apply to the rectified image:\n"
                    << "\t1. WhiteboardOrDocument\n"
                    << "\t2. Whiteboard\n"
                    << "\t3. Document\n"
                    << "\t4. Picture\n";
                std::cin >> selection;
            }
        }
        else
        {
            selection = std::stoi(__argv[3]);
            if (selection < 1 || selection > 4)
            {
                std::cout << "Invalid image cleaning preset specified, defaulting to {ImageCleaningKind.WhiteboardOrDocument.ToString()}";
                selection = (int)ImageCleaningKind::WhiteboardOrDocument + 1;
            }
        }
        imageCleaningPreset = (ImageCleaningKind)(selection - 1);

        // Set and run skill
        try
        {
            // Create the skill descriptors
            QuadDetectorDescriptor quadDetectorSkillDescriptor;
            ImageRectifierDescriptor imageRectifierSkillDescriptor;
            ImageCleanerDescriptor imageCleanerSkillDescriptor;

            // Create instance of the skills
            auto quadDetectorSkill = quadDetectorSkillDescriptor.CreateSkillAsync().get().as<QuadDetectorSkill>();
            auto imageRectifierSkill = imageRectifierSkillDescriptor.CreateSkillAsync().get().as<ImageRectifierSkill>();
            auto imageCleanerSkill = imageCleanerSkillDescriptor.CreateSkillAsync().get().as<ImageCleanerSkill>();
            auto skillDevice = quadDetectorSkill.Device();
            std::cout << "Running Skill on : " << SkillExecutionDeviceKindLookup.at(skillDevice.ExecutionDeviceKind());
            std::wcout << L" : " << skillDevice.Name().c_str() << std::endl;
            std::wcout << L"Image file: " << fileName.c_str() << std::endl;
            std::cout << "ImageInterpolationKind: " << ImageInterpolationKindLookup.at(imageInterpolationKind) << std::endl;
            std::cout << "ImageCleaningPreset: " << ImageCleaningKindLookup.at(imageCleaningPreset) << std::endl;

            // ### 1. Quad detection ###
            // Create instance of QuadDetectorBinding and set features
            auto quadDetectorBinding = quadDetectorSkill.CreateSkillBindingAsync().get().as<QuadDetectorBinding>();
            quadDetectorBinding.SetInputImageAsync(videoFrame).get();

            // Run QuadDetectorSkill
            quadDetectorSkill.EvaluateAsync(quadDetectorBinding).get();

            // ### 2. Image rectification ###
            // Create instance of ImageRectifierBinding and set input features
            auto imageRectifierBinding = imageRectifierSkill.CreateSkillBindingAsync().get().as<ImageRectifierBinding>();
            imageRectifierBinding.SetInputImageAsync(videoFrame).get();
            imageRectifierBinding.SetInputQuadAsync(quadDetectorBinding.DetectedQuads()).get();
            imageRectifierBinding.SetInterpolationKind(imageInterpolationKind);

            // Run ImageRectifierSkill
            imageRectifierSkill.EvaluateAsync(imageRectifierBinding).get();

            // ### 3. Image cleaner ###
            // Create instance of QuadDetectorBinding and set features
            auto imageCleanerBinding = imageCleanerSkill.CreateSkillBindingAsync().get().as<ImageCleanerBinding>();
            imageCleanerBinding.SetImageCleaningKindAsync(imageCleaningPreset).get();
            imageCleanerBinding.SetInputImageAsync(imageRectifierBinding.OutputImage()).get();

            // Run ImageCleanerSkill
            imageCleanerSkill.EvaluateAsync(imageCleanerBinding).get();

            // Retrieve result and save it to file
            auto results = imageCleanerBinding.OutputImage();

            auto outputFilePath = SaveModifiedVideoFrameToFile(fileName, results);
            std::wcout << L"Written output image to " << outputFilePath.c_str();

        }
        catch (hresult_error const& ex)
        {
            std::wcerr << "Error:" << ex.message().c_str() << ":" << std::hex << ex.code().value << std::endl;
            return ex.code().value;
        }
    }
    catch (hresult_error const& ex)
    {
        std::cerr << "Error:" << std::hex << ex.code() << ":\n" << ex.message().c_str();
        return  ex.code().value;;
    }
    return 0;
}