// Copyright (c) Microsoft Corporation. All rights reserved.
#pragma once

#include <winrt/Windows.Media.h>
#include <winrt/windows.media.capture.h>
#include <winrt/windows.media.capture.frames.h>
#include <winrt/Windows.Devices.Enumeration.h>
#include <winrt/windows.system.threading.h>

using namespace winrt::Windows::Media;
using namespace winrt::Windows::Media::Capture;
using namespace winrt::Windows::Media::Capture::Frames;

//
// Helper class to initialize a basic camera pipeline
//
class CameraHelper
{
public:
    static CameraHelper* CreateCameraHelper(winrt::delegate<std::string> failureHandler, winrt::delegate<VideoFrame> newFrameArrivedHandler);
    void Cleanup();
    
private:
    CameraHelper(){};
    void Initialize();
    void FirstFrameArrivedHandler(MediaFrameReader FrameReader, MediaFrameArrivedEventArgs);
    void FrameArrivedHandler(MediaFrameReader FrameReader, MediaFrameArrivedEventArgs);
    void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs);

    MediaCapture m_mediaCapture = nullptr;
    MediaCaptureSharingMode m_sharingMode = MediaCaptureSharingMode::ExclusiveControl;
    MediaFrameReader m_frameReader = nullptr;
    int m_firstFrameReceived = 0;
    winrt::event<winrt::delegate<VideoFrame>> m_signalFrameAvailable;
    winrt::event<winrt::delegate<std::string>> m_signalFailure;
    winrt::event_token m_frameArrivedEventToken;
    winrt::event_token m_failureEventToken;
    winrt::slim_mutex lock;
};