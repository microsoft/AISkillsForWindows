// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "FaceSentimentAnalyzerDescriptor.g.h"

namespace winrt::FaceSentimentAnalyzer::implementation
{
    struct FaceSentimentAnalyzerDescriptor : FaceSentimentAnalyzerDescriptorT<FaceSentimentAnalyzerDescriptor>
    {
        FaceSentimentAnalyzerDescriptor();

        winrt::guid Id();
        hstring Name();
        hstring Description();
        Microsoft::AI::Skills::SkillInterfacePreview::SkillVersion Version();
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> InputFeatureDescriptors();
        Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> OutputFeatureDescriptors();
        Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVectorView<Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice>> GetSupportedExecutionDevicesAsync();
        Windows::Foundation::Collections::IMapView<hstring, hstring> Metadata();
        Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterfacePreview::ISkill> CreateSkillAsync(Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice const executionDevice);

    private:
        Microsoft::AI::Skills::SkillInterfacePreview::SkillVersion m_version = nullptr;
        Windows::Foundation::Collections::IVector<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> m_inputSkillDesc;
        Windows::Foundation::Collections::IVector<Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeatureDescriptor> m_outputSkillDesc;
        Windows::Foundation::Collections::IVector<Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice> m_devices;
    };
}

namespace winrt::FaceSentimentAnalyzer::factory_implementation
{
    struct FaceSentimentAnalyzerDescriptor : FaceSentimentAnalyzerDescriptorT<FaceSentimentAnalyzerDescriptor, implementation::FaceSentimentAnalyzerDescriptor>
    {
    };
}
