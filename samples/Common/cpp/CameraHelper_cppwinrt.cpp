// Copyright (c) Microsoft Corporation. All rights reserved.
#include "CameraHelper_cppwinrt.h"
#include <Mferror.h>
#include <algorithm>
#include <iostream>
#include <memory>
#include <winerror.h>
#include <winrt\Windows.Foundation.Collections.h>
#include <winrt\Windows.Foundation.h>
#include <winrt\Windows.Media.MediaProperties.h>

using namespace winrt;
using namespace winrt::Windows::Media;
using namespace winrt::Windows::Media::Capture;
using namespace winrt::Windows::Media::Capture::Frames;
using namespace winrt::Windows::Media::MediaProperties;

//
// Helper method to convert a hstring to an upper case string
//
std::string ToUpperString(winrt::hstring value)
{
    auto result = winrt::to_string(value);
    std::transform(result.begin(), result.end(), result.begin(), ::toupper);
    return result;
}

//
// CameraHelper factory method that regsiters a callback for when new VideoFrames become available
//
CameraHelper* CameraHelper::CreateCameraHelper(winrt::delegate<std::string> failureHandler, winrt::delegate<VideoFrame> newFrameArrivedHandler)
{
    if (failureHandler == nullptr)
    {
        throw hresult_invalid_argument(L"Error: attempting to intialize camera with a null failureHandler");
    }
    if (newFrameArrivedHandler == nullptr)
    {
        throw hresult_invalid_argument(L"Error: attempting to intialize camera with a null FrameArrivedHandler");
    }

    CameraHelper* instance = new CameraHelper;
    try
    {
        instance->m_signalFailure.add(failureHandler);
        instance->m_signalFrameAvailable.add(newFrameArrivedHandler);
        instance->Initialize();
    }
    catch (...)
    {
        if (instance != nullptr)
        {
            delete instance;
        }
        throw;
    }

    return instance;
}

//
// Initialize camera pipeline resources and register a callback for when new VideoFrames become available.
//
void CameraHelper::Initialize()
{
    // Initialize MediaCapture with default settings in video-only streaming mode.
    // We first try to aquire exclusive sharing mode and if we fail, we then attempt again in shared mode
    // so that multiple instances can access the camera concurrently
    m_mediaCapture = MediaCapture();
    auto mediaCaptureInitializationSettings = MediaCaptureInitializationSettings();
    mediaCaptureInitializationSettings.SharingMode(m_sharingMode);
    mediaCaptureInitializationSettings.StreamingCaptureMode(StreamingCaptureMode::Video);

    // Register a callback in case MediaCapture fails. This can happen for example if another app is using the camera and we can't get ExclusiveControl
    m_mediaCapture.Failed({ this, &CameraHelper::MediaCapture_Failed });

    // This call will throw if there are no cameras attached
    m_mediaCapture.InitializeAsync(mediaCaptureInitializationSettings).get();

    // Get a list of available frame sources and iterate through them to find a video preview or 
    // a video record source with color images (and not IR, depth or other types)
    auto frameSourceIterator = m_mediaCapture.FrameSources().First();
    const wchar_t* cameraName;

    // Look first for a MediaStreamType::VideoPreview source
    auto mediaStreamTypeToLookFor = MediaStreamType::VideoPreview;
    while (frameSourceIterator.HasCurrent())
    {
        auto mediaStreamType = frameSourceIterator.Current().Value().Info().MediaStreamType();
        auto sourceKind = frameSourceIterator.Current().Value().Info().SourceKind();
        cameraName = frameSourceIterator.Current().Value().Info().DeviceInformation().Name().c_str();

        std::wcout << cameraName << L" | MediaStreamType:" << (int)mediaStreamType << L" MediaFrameSourceKind:" << (int)sourceKind << std::endl;
        if ((mediaStreamType == mediaStreamTypeToLookFor) && (sourceKind == MediaFrameSourceKind::Color))
        {
            break;
        }

        // If we reach the end of the collection and did not find a source with MediaStreamType::VideoPreview
        if (!frameSourceIterator.MoveNext() && mediaStreamTypeToLookFor == MediaStreamType::VideoPreview)
        {
            // Look for a MediaStreamType::VideoRecord source (1-pin cameras often expose only this type)
            mediaStreamTypeToLookFor = MediaStreamType::VideoRecord;
            frameSourceIterator = m_mediaCapture.FrameSources().First();
        }
    }

    if (!frameSourceIterator.HasCurrent())
    {
        std::cerr << "No valid video frame sources were found with source type color.";
        winrt::throw_hresult(MF_E_INVALIDMEDIATYPE);
    }

    // If initializing in ExclusiveControl mode, attempt to use a 15fps+ BGRA8 format natively from the camera.
    // If not, just use whatever format is already set.
    auto selectedFrameSource = frameSourceIterator.Current().Value();
    MediaFrameFormat selectedFormat = selectedFrameSource.CurrentFormat();
    if (m_sharingMode == MediaCaptureSharingMode::ExclusiveControl)
    {
        auto mediaFrameFormats = selectedFrameSource.SupportedFormats();
        std::vector<MediaFrameFormat> sortedMediaFrameFormats;
        for (auto&& format : mediaFrameFormats)
        {
            sortedMediaFrameFormats.push_back(format);
        }

        // Sort supported format by descending order of resolution
        std::sort(
            sortedMediaFrameFormats.begin(),
            sortedMediaFrameFormats.end(),
            [&](const MediaFrameFormat& format1, const MediaFrameFormat& format2) -> bool {
                return format1.VideoFormat().Width()* format1.VideoFormat().Height() > format2.VideoFormat().Width()* format2.VideoFormat().Height();
            });

        // Find a format in Bgra at 15+fps
        auto compatibleFormat = std::find_if(
            sortedMediaFrameFormats.begin(),
            sortedMediaFrameFormats.end(),
            [&](const MediaFrameFormat & format) -> bool {
                std::string formatUTF8 = ToUpperString(format.Subtype());
                return format.FrameRate().Numerator() / format.FrameRate().Denominator() >= 15 //fps
                    && 0 == formatUTF8.compare(ToUpperString(MediaEncodingSubtypes::Bgra8()));
            });

        // If not possible, then try to use other supported format at 15fps+
        if (compatibleFormat == sortedMediaFrameFormats.end())
        {
            compatibleFormat = std::find_if(
                sortedMediaFrameFormats.begin(),
                sortedMediaFrameFormats.end(),
                [&](const MediaFrameFormat & format) -> bool {
                    std::string formatUTF8 = ToUpperString(format.Subtype());
                    return format.FrameRate().Numerator() / format.FrameRate().Denominator() >= 15 //fps
                        && (0 == formatUTF8.compare(ToUpperString(MediaEncodingSubtypes::Nv12())) || 0 == formatUTF8.compare(ToUpperString(MediaEncodingSubtypes::Yuy2())) || 0 == formatUTF8.compare(ToUpperString(MediaEncodingSubtypes::Rgb32())));
                });
        }
        if (compatibleFormat == sortedMediaFrameFormats.end())
        {
            std::cerr << "No suitable media format found on the selected source";
            winrt::throw_hresult(MF_E_INVALIDMEDIATYPE);
        }
        selectedFormat = *compatibleFormat;
        selectedFrameSource.SetFormatAsync(selectedFormat).get();
        selectedFormat = selectedFrameSource.CurrentFormat();

        std::wcout << "Attempting to set camera source to " << selectedFormat.Subtype().c_str()
            << " : " << std::to_wstring(selectedFormat.VideoFormat().Width()) << "x" << std::to_wstring(selectedFormat.VideoFormat().Height())
            << "@" << std::to_wstring(selectedFormat.FrameRate().Numerator() / selectedFormat.FrameRate().Denominator()) << L"fps" << std::endl;
    }

    std::wcout << "Frame source format: " << selectedFormat.Subtype().c_str()
        << " : " << std::to_wstring(selectedFormat.VideoFormat().Width()) << "x" << std::to_wstring(selectedFormat.VideoFormat().Height())
        << "@" << std::to_wstring(selectedFormat.FrameRate().Numerator() / selectedFormat.FrameRate().Denominator()) << L"fps" << std::endl;

    // Create FrameReader with the FrameSource that we selected in the loop above.
    m_frameReader = m_mediaCapture.CreateFrameReaderAsync(frameSourceIterator.Current().Value()).get();

    // Set up a delegate to handle the frames when they are ready
    m_frameArrivedEventToken = m_frameReader.FrameArrived({ this, &CameraHelper::FrameArrivedHandler});

    // Finally start the FrameReader
    m_frameReader.StartAsync().get();
}

//
// Dispose of camera pipeline resources
//
void CameraHelper::Cleanup()
{
    // Revoke callback, stop FrameReader and close instances
    if (m_frameReader != nullptr)
    {
        m_frameReader.FrameArrived(m_frameArrivedEventToken);
        m_frameReader.StopAsync().get();
        m_frameReader.Close();
        m_frameReader = nullptr;
    }
    if (m_mediaCapture != nullptr)
    {
        m_mediaCapture.Failed(m_failureEventToken);
        m_mediaCapture.Close();
        m_mediaCapture = nullptr;
    }
}

//
// Function to handle the frame when it arrives from FrameReader
// and send it back to registered new frame handler if it is valid
//
void CameraHelper::FrameArrivedHandler(MediaFrameReader FrameReader, MediaFrameArrivedEventArgs)
{
    MediaFrameReference mediaFrame(nullptr);

    // Try to get the actual Video Frame from the FrameReader
    mediaFrame = FrameReader.TryAcquireLatestFrame();
    if (mediaFrame != nullptr)
    {
        auto vmFrame = mediaFrame.VideoMediaFrame();
        if (vmFrame != nullptr)
        {
            auto videoFrame = vmFrame.GetVideoFrame();
            m_signalFrameAvailable(videoFrame);
        }
        mediaFrame.Close();
    }
}

//
// Handle MediaCapture failure
//
void CameraHelper::MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
{
    std::wcerr << L"MediaCapture failed: " << errorEventArgs.Message().c_str() << std::endl;
    Cleanup();

    // if we failed to initialize MediaCapture ExclusiveControl with MF_E_HW_MFT_FAILED_START_STREAMING,
    // let's retry in SharedReadOnly mode since this points to a camera already in use
    if (m_sharingMode == MediaCaptureSharingMode::ExclusiveControl
        && errorEventArgs.Code() == 0xc00d3704)
    {
        m_sharingMode = MediaCaptureSharingMode::SharedReadOnly;
        std::cout << "Retrying MediaCapture initialization" << std::endl;
        Initialize();
    }
    else
    {
        m_signalFailure(std::string("Camera error:") + std::to_string(errorEventArgs.Code()) + winrt::to_string(errorEventArgs.Message()));
    }
}