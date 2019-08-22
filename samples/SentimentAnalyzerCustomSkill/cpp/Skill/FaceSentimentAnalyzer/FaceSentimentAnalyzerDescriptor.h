// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "FaceSentimentAnalyzerDescriptor.g.h"

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    struct FaceSentimentAnalyzerDescriptor : FaceSentimentAnalyzerDescriptorT<FaceSentimentAnalyzerDescriptor>
    {
        FaceSentimentAnalyzerDescriptor();
        Microsoft::AI::Skills::SkillInterfacePreview::SkillInformation Information();
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> InputFeatureDescriptors();
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> OutputFeatureDescriptors();
        Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice>> GetSupportedExecutionDevicesAsync();
        Windows::Foundation::Collections::IMapView<hstring, hstring> Metadata();
        Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterfacePreview::ISkill> CreateSkillAsync();
        Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterfacePreview::ISkill> CreateSkillAsync(Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice const executionDevice);

    private:
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> m_inputSkillDesc;
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> m_outputSkillDesc;
        Windows::Foundation::Collections::IVector<Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice> m_devices;
        Microsoft::AI::Skills::SkillInterfacePreview::SkillInformation m_information = nullptr;
    };
}

namespace winrt::Contoso::FaceSentimentAnalyzer::factory_implementation
{
    struct FaceSentimentAnalyzerDescriptor : FaceSentimentAnalyzerDescriptorT<FaceSentimentAnalyzerDescriptor, implementation::FaceSentimentAnalyzerDescriptor>
    {
    };
}
