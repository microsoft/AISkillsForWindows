// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "FaceSentimentAnalyzerBinding.h"
#include "FaceSentimentAnalyzerConst.h"

using namespace winrt::Microsoft::AI::Skills::SkillInterface;
using namespace winrt::Windows::AI::MachineLearning;

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    //
    // FaceSentimentAnalyzerBinding constructor
    //
    FaceSentimentAnalyzerBinding::FaceSentimentAnalyzerBinding(
        ISkillDescriptor descriptor,
        ISkillExecutionDevice device,
        LearningModelSession session)
    {
        m_bindingHelper = VisionSkillBindingHelper(descriptor, device);
        m_winmlBinding = LearningModelBinding(session);
    }

    //
    // Returns whether or not a face is found given the bound outputs
    //
    bool FaceSentimentAnalyzerBinding::IsFaceFound()
    {
        auto faceRect = m_bindingHelper.TryLookup(SKILL_OUTPUTNAME_FACERECTANGLE).FeatureValue().as<SkillFeatureTensorFloatValue>().GetAsVectorView();
        if (faceRect != nullptr)
        {
            return !(faceRect.GetAt(0) == 0.0f &&
                faceRect.GetAt(1) == 0.0f &&
                faceRect.GetAt(2) == 0.0f &&
                faceRect.GetAt(3) == 0.0f);
        }
        return false;
    }

    //
    // Returns the sentiment with the highest score
    //
    FaceSentimentAnalyzer::SentimentType FaceSentimentAnalyzerBinding::PredominantSentiment()
    {
        auto faceSentimentScores = m_bindingHelper.TryLookup(SKILL_OUTPUTNAME_FACESENTIMENTSCORES).FeatureValue().as<SkillFeatureTensorFloatValue>().GetAsVectorView();
        SentimentType predominantSentiment = SentimentType::neutral;
        if (faceSentimentScores != nullptr)
        {
            float maxScore = FLT_MIN;
            for (uint32_t i = 0; i < faceSentimentScores.Size(); i++)
            {
                if (faceSentimentScores.GetAt(i) > maxScore)
                {
                    predominantSentiment = (SentimentType)i;
                    maxScore = faceSentimentScores.GetAt(i);
                }
            }
        }
        return predominantSentiment;
    }

    //
    // Returns the face rectangle
    //
    Windows::Foundation::Collections::IVectorView<float> FaceSentimentAnalyzerBinding::FaceRectangle()
    {
        return m_bindingHelper.TryLookup(SKILL_OUTPUTNAME_FACERECTANGLE).FeatureValue().as<SkillFeatureTensorFloatValue>().GetAsVectorView();
    }
}
