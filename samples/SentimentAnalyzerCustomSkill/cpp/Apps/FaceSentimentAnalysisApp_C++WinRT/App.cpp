// Copyright (c) Microsoft Corporation. All rights reserved.
#include "App.h"

// Function to handle the frame when it arrives from FrameReader
winrt::Windows::Foundation::IAsyncAction App::FrameArrivedHandler(MediaFrameReader FrameReader, MediaFrameArrivedEventArgs)
{
    // Lock context so multiple overlapping events from FrameReader do not race for the resources.
    m_lock.lock();
    MediaFrameReference mediaFrame(nullptr);

    // Try to get the actual Video Frame from the FrameReader
    mediaFrame = FrameReader.TryAcquireLatestFrame();
    if (mediaFrame != nullptr)
    {
        auto vmFrame = mediaFrame.VideoMediaFrame();
        if (vmFrame != nullptr)
        {
            auto videoFrame = vmFrame.GetVideoFrame();

            // Set the video frame on the skill binding.
            m_FaceSentimentSkillBinding.SetInputImageAsync(videoFrame).get();

            // Evaluate sentiments in video frame using the skill
            m_Skill.EvaluateAsync(m_FaceSentimentSkillBinding).get();
            if (m_FaceSentimentSkillBinding.IsFaceFound())
            {
                auto sentiments = m_FaceSentimentSkillBinding.PredominantSentiments();
                static const std::string classifiedSentiments[] = {
                    "neutral ",
                    "happiness ",
                    "surprise ",
                    "sadness ",
                    "anger ",
                    "disgust",
                    "fear",
                    "contempt"
                };
                std::string outText = "\tYour sentiment looks like:  " + classifiedSentiments[(int)sentiments.GetAt(0)] + "\t\t\t\t\r";
                std::cout << outText;
            }
            else
            {
                std::cout << "\tNo face detected\t\t\t\t\t\t\r";
            }
            videoFrame.Close();
        }
        mediaFrame.Close();
    }
    
    m_lock.unlock();
    co_return;
}

void App::InitCameraAndFrameSource()
{
    // Initialize media capture with default settings in video-only streaming mode and in shared mode so that multiple instances can access the camera concurrently
    m_mediaCapture = winrt::Windows::Media::Capture::MediaCapture();
    auto mediaCaptureInitializationSettings = winrt::Windows::Media::Capture::MediaCaptureInitializationSettings();
    mediaCaptureInitializationSettings.SharingMode(winrt::Windows::Media::Capture::MediaCaptureSharingMode::SharedReadOnly);
    mediaCaptureInitializationSettings.StreamingCaptureMode(winrt::Windows::Media::Capture::StreamingCaptureMode::Video);
    m_mediaCapture.InitializeAsync(mediaCaptureInitializationSettings).get();

    // Get a list of available Frame source and iterate through them to find a Video preview/record source with Color images ( and not IR/depth etc)
    auto fsIter = m_mediaCapture.FrameSources().First();
    do
    {
        auto mediaStreamType = fsIter.Current().Value().Info().MediaStreamType();
        auto sourceKind = fsIter.Current().Value().Info().SourceKind();
        auto cameraName = fsIter.Current().Value().Info().DeviceInformation().Name().c_str();

        std::cout << to_string(cameraName) << " | MediaStreamType:" << (int)mediaStreamType << " MediaFrameSourceKind:" << (int)sourceKind << std::endl;
        if (
            ((mediaStreamType == MediaStreamType::VideoPreview)
                || (mediaStreamType == MediaStreamType::VideoRecord))
            && (sourceKind == MediaFrameSourceKind::Color))
        {
            break;
        }
    } while (fsIter.MoveNext());
    if (!fsIter.HasCurrent())
    {
        std::cout << "No valid video frame sources were found with source type color.";
        throw new winrt::hresult(MF_E_INVALIDMEDIATYPE);
    }

    // Create FrameReader with the FrameSource that we selected in the loop above.
    m_frameReader = m_mediaCapture.CreateFrameReaderAsync(fsIter.Current().Value()).get();

    // Set up a delegate to handle the frames when they are ready
    m_frameReader.FrameArrived({ this, &App::FrameArrivedHandler });

    // Finally start the FrameReader
    m_frameReader.StartAsync().get();
}

void App::DeInitCameraAndFrameSource()
{
    //Stop FrameReader and close
    m_frameReader.StopAsync().get();
    m_frameReader.Close();
    m_frameReader = nullptr;
    m_mediaCapture.Close();
    m_mediaCapture = nullptr;
}

int App::AppMain()
{
    std::cout << "C++/WinRT Non-packaged(win32) console APP: Please face your camera" << std::endl;

    try 
    {
        // Create the FaceSentimentAnalyzer skill descriptor
        auto skillDesc = FaceSentimentAnalyzerDescriptor().as<ISkillDescriptor>();

        // Create instance of the skill
        m_Skill = skillDesc.CreateSkillAsync().get().as<FaceSentimentAnalyzerSkill>();

        // Create instance of the skill binding
        m_FaceSentimentSkillBinding = m_Skill.CreateSkillBindingAsync().get().as<FaceSentimentAnalyzerBinding>();

        // Initialize MediaCapture and FrameReader
        InitCameraAndFrameSource();
        std::cout << "\t\t\t\t\t\t\t\t...press enter to Stop" << std::endl;

        // Wait for enter keypress
        while (std::cin.get() != '\n');

        std::cout << std::endl << "Key pressed.. exiting";

        // De-initialize the MediaCapture and FrameReader
        DeInitCameraAndFrameSource();
    }
    catch (hresult_error const &ex)
    {
        std::wcout << "Error:" << ex.message().c_str() << ":" << std::hex << ex.code().value << std::endl;
        return ex.code().value;
    }
    return 0;
}

