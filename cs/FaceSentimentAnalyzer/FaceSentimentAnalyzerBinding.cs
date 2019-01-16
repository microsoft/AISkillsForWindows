// Copyright (c) Microsoft Corporation. All rights reserved. 

using System.Collections;
using System.Collections.Generic;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Windows.AI.MachineLearning;
using System.Linq;
using Windows.Foundation;
using Windows.Media;

#pragma warning disable CS1591 // Disable missing comment warning

namespace FaceSentimentAnalyzer
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
    public sealed  class FaceSentimentAnalyzerBinding : IReadOnlyDictionary<string, ISkillFeature>, ISkillBinding
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
                var faceRect = (m_bindingHelper[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACERECTANGLE].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                return !(faceRect[0] == 0.0f &&
                    faceRect[1] == 0.0f &&
                    faceRect[2] == 0.0f &&
                    faceRect[3] == 0.0f);
            }
        }

        /// <summary>
        /// Returns the sentiment with the highest score
        /// </summary>
        /// <returns></returns>
        public SentimentType PredominantSentiment
        {
            get
            {
                var faceSentimentScores = (m_bindingHelper[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACESENTIMENTSCORES].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                SentimentType predominantSentiment = SentimentType.neutral;
                float maxScore = float.MinValue;
                for (int i = 0; i < faceSentimentScores.Count; i++)
                {
                    if (faceSentimentScores[i] > maxScore)
                    {
                        predominantSentiment = (SentimentType)i;
                        maxScore = faceSentimentScores[i];
                    }

                }
                return predominantSentiment;
            }
        }

        /// <summary>
        /// Returns the face rectangle
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<float> FaceRectangle
        {
            get
            {
                return (m_bindingHelper[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACERECTANGLE].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
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
