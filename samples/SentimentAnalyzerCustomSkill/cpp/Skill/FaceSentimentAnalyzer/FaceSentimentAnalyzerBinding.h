// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "FaceSentimentAnalyzerBinding.g.h"
#include "FaceSentimentAnalyzerSkill.h"

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    struct FaceSentimentAnalyzerBinding : FaceSentimentAnalyzerBindingT<FaceSentimentAnalyzerBinding>
    {
        FaceSentimentAnalyzerBinding(
            Microsoft::AI::Skills::SkillInterfacePreview::ISkillDescriptor descriptor,
            Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice device,
            Windows::AI::MachineLearning::LearningModelSession session);

        Windows::Foundation::IAsyncAction SetInputImageAsync(Windows::Media::VideoFrame videoFrame)
        {
            return m_bindingHelper.SetInputImageAsync(videoFrame);
        }

        bool IsFaceFound();
        Contoso::FaceSentimentAnalyzer::SentimentType PredominantSentiment();
        Windows::Foundation::Collections::IVectorView<float> FaceRectangle();

        // interface implementation via the VisionSkillBindingHelper member instance
#pragma region InterfaceImpl
        using KeyValuePair =
            Windows::Foundation::Collections::IKeyValuePair<hstring, Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeature>;

        Microsoft::AI::Skills::SkillInterfacePreview::ISkillExecutionDevice Device()
        {
            return m_bindingHelper.Device();
        }

        void Clear() { m_bindingHelper.Clear(); }

        Windows::Foundation::Collections::IIterator<KeyValuePair> First()
        {
            return m_bindingHelper.First();
        }

        Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeature Lookup(hstring const& key)
        {
            return m_bindingHelper.Lookup(key);
        }

        uint32_t Size() { return m_bindingHelper.Size(); }

        bool HasKey(hstring const& key)
        {
            return m_bindingHelper.HasKey(key);
        }
        void Split(
            Windows::Foundation::Collections::IMapView<hstring, Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeature>& first,
            Windows::Foundation::Collections::IMapView<hstring, Microsoft::AI::Skills::SkillInterfacePreview::ISkillFeature>& second)
        {
            m_bindingHelper.GetView().Split(first, second);
        }
#pragma endregion InterfaceImpl

    private:
        Windows::AI::MachineLearning::LearningModelBinding m_winmlBinding = nullptr;
        Microsoft::AI::Skills::SkillInterfacePreview::VisionSkillBindingHelper m_bindingHelper = nullptr;

        friend struct winrt::Contoso::FaceSentimentAnalyzer::implementation::FaceSentimentAnalyzerSkill;
    };
}
