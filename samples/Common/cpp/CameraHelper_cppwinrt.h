// Copyright (c) Microsoft Corporation. All rights reserved.
#pragma once

#include <winrt/Windows.Media.h>
#include <winrt/windows.media.capture.h>
#include <winrt/windows.media.capture.frames.h>
#include <winrt/Windows.Devices.Enumeration.h>
#include <winrt/windows.system.threading.h>

//
// Helper class to initialize a basic camera pipeline
//
class CameraHelper
{
public:
    static CameraHelper* CreateCameraHelper(winrt::delegate<std::string> failureHandler, winrt::delegate<winrt::Windows::Media::VideoFrame> newFrameArrivedHandler);
    void Cleanup();
    
private:
    CameraHelper(){};
    void Initialize();
    void FrameArrivedHandler(winrt::Windows::Media::Capture::Frames::MediaFrameReader FrameReader, winrt::Windows::Media::Capture::Frames::MediaFrameArrivedEventArgs);
    void MediaCapture_Failed(winrt::Windows::Media::Capture::MediaCapture sender, winrt::Windows::Media::Capture::MediaCaptureFailedEventArgs errorEventArgs);

    winrt::Windows::Media::Capture::MediaCapture m_mediaCapture = nullptr;
    winrt::Windows::Media::Capture::MediaCaptureSharingMode m_sharingMode = winrt::Windows::Media::Capture::MediaCaptureSharingMode::ExclusiveControl;
    winrt::Windows::Media::Capture::Frames::MediaFrameReader m_frameReader = nullptr;
    int m_firstFrameReceived = 0;
    winrt::event<winrt::delegate<winrt::Windows::Media::VideoFrame>> m_signalFrameAvailable;
    winrt::event<winrt::delegate<std::string>> m_signalFailure;
    winrt::event_token m_frameArrivedEventToken;
    winrt::event_token m_failureEventToken;
    winrt::slim_mutex lock;
};