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
        auto faceRect = m_bindingHelper.TryLookup(SKILL_OUTPUTNAME_FACEBOUNDINGBOXES).FeatureValue().as<SkillFeatureTensorFloatValue>().GetAsVectorView();
        return faceRect.Size() > 0;
    }

    //
    // Returns the sentiments with the highest score
    //
    Windows::Foundation::Collections::IVectorView<FaceSentimentAnalyzer::SentimentType> FaceSentimentAnalyzerBinding::PredominantSentiments()
    {
        auto faceSentimentScores = m_bindingHelper.TryLookup(SKILL_OUTPUTNAME_FACESENTIMENTSSCORES).FeatureValue().as<SkillFeatureTensorFloatValue>().GetAsVectorView();
        auto predominantSentiments = single_threaded_vector<SentimentType>();
        for (uint32_t i = 0; i < faceSentimentScores.Size(); i += ((int)SentimentType::contempt + 1))
        {
            float maxScore = FLT_MIN;
            SentimentType predominantSentiment = SentimentType::neutral;
            for (int j = 0; j < ((int)SentimentType::contempt + 1); j++)
            {
                float score = faceSentimentScores.GetAt(i + j);
                if (score > maxScore)
                {
                    predominantSentiment = (SentimentType)j;
                    if (score >= 0.5f) // since scores are softmax, there can't be 2 scores above 0.5, early break opportunity
                    {
                        break;
                    }
                    maxScore = score;
                }
            }
            predominantSentiments.Append(predominantSentiment);
        }
        return predominantSentiments.GetView();
    }

    //
    // Returns the face boundingBoxes
    //
    Windows::Foundation::Collections::IVectorView<float> FaceSentimentAnalyzerBinding::FaceBoundingBoxes()
    {
        return m_bindingHelper.TryLookup(SKILL_OUTPUTNAME_FACEBOUNDINGBOXES).FeatureValue().as<SkillFeatureTensorFloatValue>().GetAsVectorView();
    }
}
