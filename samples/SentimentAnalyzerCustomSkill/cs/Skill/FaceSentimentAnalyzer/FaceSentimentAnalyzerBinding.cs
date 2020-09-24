// Copyright (c) Microsoft Corporation. All rights reserved. 

using System.Collections;
using System.Collections.Generic;
using Microsoft.AI.Skills.SkillInterface;
using Windows.AI.MachineLearning;
using System.Linq;
using Windows.Foundation;
using Windows.Media;

#pragma warning disable CS1591 // Disable missing comment warning

namespace Contoso.FaceSentimentAnalyzer
{
    /// <summary>
    /// Defines the set of possible emotion label scored by this skill
    /// </summary>
    public enum SentimentType
    {
        neutral = 0,
        happiness,
        surprise,
        sadness,
        anger,
        disgust,
        fear,
        contempt
    };

    /// <summary>
    /// FaceSentimentAnalyzerBinding class.
    /// It holds the input and output passed and retrieved from a FaceSentimentAnalyzerSkill instance.
    /// </summary>
    public sealed class FaceSentimentAnalyzerBinding : IReadOnlyDictionary<string, ISkillFeature>, ISkillBinding
    {
        // WinML related
        internal LearningModelBinding m_winmlBinding = null;
        private VisionSkillBindingHelper m_bindingHelper = null;

        /// <summary>
        /// FaceSentimentAnalyzerBinding constructor
        /// </summary>
        internal FaceSentimentAnalyzerBinding(
            ISkillDescriptor descriptor, 
            ISkillExecutionDevice device, 
            LearningModelSession session)
        {
            m_bindingHelper = new VisionSkillBindingHelper(descriptor, device);

            // Create WinML binding
            m_winmlBinding = new LearningModelBinding(session);
        }

        /// <summary>
        /// Sets the input image to be processed by the skill
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public IAsyncAction SetInputImageAsync(VideoFrame frame)
        {
            return m_bindingHelper.SetInputImageAsync(frame);
        }

        /// <summary>
        /// Returns whether or not a face is found given the bound outputs
        /// </summary>
        public bool IsFaceFound
        {
            get
            {
                ISkillFeature feature = m_bindingHelper[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACEBOUNDINGBOXES];
                var faceBoundingBoxes = (feature.FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                return faceBoundingBoxes.Count > 0;
            }
        }

        /// <summary>
        /// Returns the sentiments with the highest score for each face detected
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<SentimentType> PredominantSentiments
        {
            get
            {
                var faceSentimentScores = (m_bindingHelper[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACESENTIMENTSSCORES].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                var predominantSentiments = new List<SentimentType>();
               
                for (int i = 0; i < faceSentimentScores.Count; i += ((int)SentimentType.contempt + 1))
                {
                    float maxScore = float.MinValue;
                    var predominantSentiment = SentimentType.neutral;
                    for (int j = 0; j < ((int)SentimentType.contempt + 1); j++)
                    {
                        float score = faceSentimentScores[i + j];
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
                    predominantSentiments.Add(predominantSentiment);
                }
                return predominantSentiments;
            }
        }

        /// <summary>
        /// Returns the bounding boxes around each detected face
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<float> FaceBoundingBoxes
        {
            get
            {
                ISkillFeature feature = m_bindingHelper[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACEBOUNDINGBOXES];
                return (feature.FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
            }
        }

        // interface implementation via composition
#region InterfaceImpl

        // ISkillBinding
        public ISkillExecutionDevice Device => m_bindingHelper.Device;


        // IReadOnlyDictionary
        public bool ContainsKey(string key)
        {
            return m_bindingHelper.ContainsKey(key);
        }

        public bool TryGetValue(string key, out ISkillFeature value)
        {
            return m_bindingHelper.TryGetValue(key, out value);
        }

        public ISkillFeature this[string key] => m_bindingHelper[key];

        public IEnumerable<string> Keys => m_bindingHelper.Keys;

        public IEnumerable<ISkillFeature> Values => m_bindingHelper.Values;

        public int Count => m_bindingHelper.Count;

        public IEnumerator<KeyValuePair<string, ISkillFeature>> GetEnumerator()
        {
            return m_bindingHelper.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_bindingHelper.AsEnumerable().GetEnumerator();
        }

#endregion InterfaceImpl
        // end of implementation of interface via composition
    }
}
