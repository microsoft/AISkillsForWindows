// Copyright (c) Microsoft Corporation. All rights reserved.
#pragma once
#include <iostream>
#include <string>
#include <winerror.h>
#include <Mferror.h>
#include <winrt/Windows.Devices.Enumeration.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/windows.foundation.collections.h>
#include <winrt/windows.system.threading.h>
#include <winrt/windows.media.h>
#include <winrt/windows.media.capture.h>
#include <winrt/windows.media.capture.frames.h>
#include <winrt/Windows.System.Threading.h>
#include "winrt/Contoso.FaceSentimentAnalyzer.h"
#include "winrt/Microsoft.AI.Skills.SkillInterface.h"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::System::Threading;
using namespace winrt::Windows::Media;
using namespace winrt::Windows::Media::Capture;
using namespace winrt::Windows::Media::Capture::Frames;
using namespace Microsoft::AI::Skills::SkillInterface;
using namespace Contoso::FaceSentimentAnalyzer;

class App
{
    MediaCapture m_mediaCapture;
    FaceSentimentAnalyzerBinding m_FaceSentimentSkillBinding;
    FaceSentimentAnalyzerSkill m_Skill;
    MediaFrameReader m_frameReader;
    winrt::slim_mutex m_lock;
    void InitCameraAndFrameSource();
    void DeInitCameraAndFrameSource();
    winrt::Windows::Foundation::IAsyncAction FrameArrivedHandler(MediaFrameReader pFrameReader, MediaFrameArrivedEventArgs);

public:
    App()
        : m_frameReader(nullptr)
        , m_mediaCapture(nullptr)
        , m_FaceSentimentSkillBinding(nullptr)
        , m_Skill(nullptr)
    {

    }
    int AppMain();
};