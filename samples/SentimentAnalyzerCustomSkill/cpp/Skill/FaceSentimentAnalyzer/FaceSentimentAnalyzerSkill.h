// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "FaceSentimentAnalyzerSkill.g.h"

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    struct FaceSentimentAnalyzerSkill : FaceSentimentAnalyzerSkillT<FaceSentimentAnalyzerSkill>
    {
        static Windows::Foundation::IAsyncOperation<winrt::Contoso::FaceSentimentAnalyzer::FaceSentimentAnalyzerSkill> CreateAsync(
            Microsoft::AI::Skills::SkillInterface::ISkillDescriptor descriptor,
            Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice device);

        FaceSentimentAnalyzerSkill(
            Microsoft::AI::Skills::SkillInterface::ISkillDescriptor descriptor,
            Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice device,
            Windows::Media::FaceAnalysis::FaceDetector faceDetector,
            Windows::AI::MachineLearning::LearningModelSession winmlSession)
            : m_skillDescriptor(descriptor), 
            m_device(device),
            m_faceDetector(faceDetector),
            m_winmlSession(winmlSession)
        {}

        Microsoft::AI::Skills::SkillInterface::ISkillDescriptor SkillDescriptor()
        {
            return m_skillDescriptor;
        }

        Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice Device()
        {
            return m_device;
        }

        Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterface::ISkillBinding> CreateSkillBindingAsync();

        Windows::Foundation::IAsyncAction EvaluateAsync(Microsoft::AI::Skills::SkillInterface::ISkillBinding const binding);

    private:

        // Face detection related members
        Windows::Media::FaceAnalysis::FaceDetector m_faceDetector = nullptr;

        // WinML related members
        Windows::AI::MachineLearning::LearningModelSession m_winmlSession = nullptr;

        Microsoft::AI::Skills::SkillInterface::ISkillDescriptor m_skillDescriptor;
        const Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice m_device;
    };
}
