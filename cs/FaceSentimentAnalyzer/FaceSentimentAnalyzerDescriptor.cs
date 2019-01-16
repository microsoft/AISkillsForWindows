// Copyright (c) Microsoft Corporation. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace FaceSentimentAnalyzer
{
    /// <summary>
    /// FaceSentimentAnalyzerDescriptor class
    /// Exposes information about the skill and its input/output variable requirements.
    /// Also acts as a factory for FaceSentimentAnalyzerSkill
    /// </summary>
    public sealed class FaceSentimentAnalyzerDescriptor : ISkillDescriptor
    {
        // Member variables
        private List<ISkillFeatureDescriptor> m_inputSkillDesc;
        private List<ISkillFeatureDescriptor> m_outputSkillDesc;

        /// <summary>
        /// FaceSentimentAnalyzerDescriptor constructor
        /// </summary>
        public FaceSentimentAnalyzerDescriptor()
        {
            Name = "FaceSentimentAnalyzer";

            Description = "Finds a face in the image and infers its predominant sentiment from a set of 8 possible labels";
           
            // {F8D275CE-C244-4E71-8A39-57335D291388}
            Id = new Guid(0xf8d275ce, 0xc244, 0x4e71, 0x8a, 0x39, 0x57, 0x33, 0x5d, 0x29, 0x13, 0x88);

            Version = SkillVersion.Create(
                0,  // major version
                1,  // minor version
                "Contoso Developer", // Author name 
                "Contoso Publishing" // Publisher name
                ); 

            // Describe input feature
            m_inputSkillDesc = new List<ISkillFeatureDescriptor>();
            m_inputSkillDesc.Add(
                SkillFeatureImageDescriptor.Create(
                    FaceSentimentAnalyzerConst.SKILL_INPUTNAME_IMAGE,
                    "the input image onto which the sentiment analysis runs",
                    true, // isRequired (since this is an input, it is required to be bound before the evaluation occurs)
                    -1, // width
                    -1, // height
                    -1, // maxDimension
                    BitmapPixelFormat.Nv12,
                    BitmapAlphaMode.Ignore)
            );

            // Describe first output feature
            m_outputSkillDesc = new List<ISkillFeatureDescriptor>();
            m_outputSkillDesc.Add(
                SkillFeatureTensorDescriptor.Create(
                    FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACERECTANGLE,
                    "a face bounding box in relative coordinates (left, top, right, bottom)",
                    false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                    new List<long>() { 4 }, // tensor shape
                    SkillElementKind.Float)
                );

            // Describe second output feature
            m_outputSkillDesc.Add(
                SkillFeatureTensorDescriptor.Create(
                    FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACESENTIMENTSCORES,
                    "the prediction score for each class",
                    false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                    new List<long>() { 1, 8 }, // tensor shape
                    SkillElementKind.Float)
                );
        }

        /// <summary>
        /// Retrieves a list of supported ISkillExecutionDevice to run the skill logic on
        /// </summary>
        /// <returns></returns>
        public IAsyncOperation<IReadOnlyList<ISkillExecutionDevice>> GetSupportedExecutionDevicesAsync()
        {
            return AsyncInfo.Run(async (token) =>
            {
                var result = new List<ISkillExecutionDevice>();
                await Task.Run(() =>
                {
                    // Add CPU as supported device
                    result.Add(SkillExecutionDeviceCPU.Create());

                    // Retrieve a list of GPUs available on the system and filter them bv keeping only GPUs that support DX11+ feature level
                    var gpuDevices = SkillExecutionDeviceGPU.GetAvailableGpuExecutionDevices();
                    var compatibleGpuDevices = gpuDevices.Where((device) => (device as SkillExecutionDeviceGPU).MaxSupportedFeatureLevel >= D3DFeatureLevelKind.D3D_FEATURE_LEVEL_11_0);
                    result.AddRange(compatibleGpuDevices);
                });
                return result as IReadOnlyList<ISkillExecutionDevice>;
            });
        }

        /// <summary>
        /// Factory method for instantiating and initializing the skill
        /// </summary>
        /// <param name="executionDevice"></param>
        /// <returns></returns>
        public IAsyncOperation<ISkill> CreateSkillAsync(ISkillExecutionDevice executionDevice)
        {
            return AsyncInfo.Run(async (token) =>
            {
                // Create a skill instance with the executionDevice supplied
                var skillInstance = await FaceSentimentAnalyzerSkill.CreateAsync(this, executionDevice);          

                return skillInstance as ISkill;
            });
        }

        /// <summary>
        /// Returns a description of the skill
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Returns a unique skill identifier
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Returns a list of descriptors that correlate with each input SkillFeature
        /// </summary>
        public IReadOnlyList<ISkillFeatureDescriptor> InputFeatureDescriptors => m_inputSkillDesc;

        /// <summary>
        /// Returns a set of metadata that may control the skill execution differently than by feeding an input 
        /// i.e. internal state, sub-process execution frequency, etc.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metadata => null;

        /// <summary>
        /// Returns the skill name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns a list of descriptors that correlate with each output SkillFeature
        /// </summary>
        public IReadOnlyList<ISkillFeatureDescriptor> OutputFeatureDescriptors => m_outputSkillDesc;

        /// <summary>
        /// Returns the Version information of the skill
        /// </summary>
        public SkillVersion Version { get; }
    }
}
