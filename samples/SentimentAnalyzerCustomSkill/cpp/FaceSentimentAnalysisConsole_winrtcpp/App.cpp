// Copyright (c) Microsoft Corporation. All rights reserved.
#include "App.h"

winrt::Windows::Foundation::IAsyncAction App::FrameArrivedHandler(MediaFrameReader FrameReader, MediaFrameArrivedEventArgs)
{
    m_lock.lock();
    MediaFrameReference mediaFrame(nullptr);
    mediaFrame = FrameReader.TryAcquireLatestFrame();
    if (mediaFrame != nullptr)
    {
        auto vmFrame = mediaFrame.VideoMediaFrame();
        if (vmFrame != nullptr)
        {
            auto videoFrame = vmFrame.GetVideoFrame();
            m_FaceSentimentSkillBinding.SetInputImageAsync(videoFrame).get();
            m_Skill.EvaluateAsync(m_FaceSentimentSkillBinding).get();
            if (m_FaceSentimentSkillBinding.IsFaceFound())
            {
                auto sentiment = m_FaceSentimentSkillBinding.PredominantSentiment();
                std::string sentiments[] = {
                    "neutral ",
                    "happiness ",
                    "surprise ",
                    "sadness ",
                    "anger ",
                    "disgust",
                    "fear",
                    "contempt"
                };
                std::string outText = "\tYour sentiment looks like:  " + sentiments[(int)sentiment] + "\t\t\t\t\r";
                std::cout << outText;
            }
            else
            {
                std::cout << "\tNo face detected\t\t\t\t\t\t\r";// << std::endl;
            }
            videoFrame.Close();
        }
        mediaFrame.Close();
    }
    
    m_lock.unlock();
    co_return;
}

void App::initMediaCapture()
{
    m_mediaCapture = winrt::Windows::Media::Capture::MediaCapture();
    m_mediaCapture.InitializeAsync().get();
    auto fsIter = m_mediaCapture.FrameSources().First();
    do
    {
        auto mediaStreamType = fsIter.Current().Value().Info().MediaStreamType();
        auto sourceKind = fsIter.Current().Value().Info().SourceKind();
        std::cout << "MediaStreamType:" << (int)mediaStreamType << " MediaFrameSourceKind:" << (int)sourceKind << std::endl;
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
        throw new winrt::hresult_out_of_bounds();
    }

    m_frameReader = m_mediaCapture.CreateFrameReaderAsync(fsIter.Current().Value()).get();
    m_frameReader.FrameArrived({ this, &App::FrameArrivedHandler });
    m_frameReader.StartAsync().get();
}

void App::deInitMediaCapture()
{
    m_frameReader.StopAsync().get();
    m_frameReader.Close();
    m_frameReader = nullptr;
    m_mediaCapture.Close();
    m_mediaCapture = nullptr;
}

int App::AppMain()
{
    std::cout << "WinrtCPP Non-packaged(win32) console APP: Face it!!" << std::endl;
    auto skillDesc = FaceSentimentAnalyzerDescriptor().as<ISkillDescriptor>();
    m_Skill = skillDesc.CreateSkillAsync().get().as<FaceSentimentAnalyzerSkill>();
    m_FaceSentimentSkillBinding = m_Skill.CreateSkillBindingAsync().get().as<FaceSentimentAnalyzerBinding>();

    initMediaCapture();
    std::cout << "\t\t\t\t\t\t\t\t...press enter to Stop" << std::endl;
    
    //wait for enter keypress
    while (std::cin.get() != '\n');
    std::cout << std::endl << "Key pressed.. exiting";
    deInitMediaCapture();
}

