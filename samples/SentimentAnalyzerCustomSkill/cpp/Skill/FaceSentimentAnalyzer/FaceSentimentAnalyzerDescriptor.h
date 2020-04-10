// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "FaceSentimentAnalyzerDescriptor.g.h"

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    struct FaceSentimentAnalyzerDescriptor : FaceSentimentAnalyzerDescriptorT<FaceSentimentAnalyzerDescriptor>
    {
        FaceSentimentAnalyzerDescriptor();
        Microsoft::AI::Skills::SkillInterface::SkillInformation Information();
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterface::ISkillFeatureDescriptor> InputFeatureDescriptors();
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterface::ISkillFeatureDescriptor> OutputFeatureDescriptors();
        Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice>> GetSupportedExecutionDevicesAsync();
        Windows::Foundation::Collections::IMapView<hstring, hstring> Metadata();
        Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterface::ISkill> CreateSkillAsync();
        Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterface::ISkill> CreateSkillAsync(Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice const executionDevice);

    private:
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterface::ISkillFeatureDescriptor> m_inputSkillDesc;
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterface::ISkillFeatureDescriptor> m_outputSkillDesc;
        Windows::Foundation::Collections::IVector<Microsoft::AI::Skills::SkillInterface::ISkillExecutionDevice> m_devices;
        Microsoft::AI::Skills::SkillInterface::SkillInformation m_information = nullptr;
    };
}

namespace winrt::Contoso::FaceSentimentAnalyzer::factory_implementation
{
    struct FaceSentimentAnalyzerDescriptor : FaceSentimentAnalyzerDescriptorT<FaceSentimentAnalyzerDescriptor, implementation::FaceSentimentAnalyzerDescriptor>
    {
    };
}
