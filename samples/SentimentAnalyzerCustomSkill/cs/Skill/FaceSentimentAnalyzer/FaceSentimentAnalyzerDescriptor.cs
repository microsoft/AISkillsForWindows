// Copyright (c) Microsoft Corporation. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.AI.Skills.SkillInterface;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace Contoso.FaceSentimentAnalyzer
{
    /// <summary>
    /// FaceSentimentAnalyzerDescriptor class.
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
            Information = SkillInformation.Create(
                "FaceSentimentAnalyzer", // Name
                "Finds a face in the image and infers its predominant sentiment from a set of 8 possible labels", // Description
                new Guid(0xf8d275ce, 0xc244, 0x4e71, 0x8a, 0x39, 0x57, 0x33, 0x5d, 0x29, 0x13, 0x88), // Id
                new Windows.ApplicationModel.PackageVersion() { Major = 0, Minor = 0, Build = 0, Revision = 8 }, // Version
                "Contoso Developer", // Author
                "Contoso Publishing" // Publisher
            ); 

            // Describe input feature
            m_inputSkillDesc = new List<ISkillFeatureDescriptor>();
            m_inputSkillDesc.Add(
                new SkillFeatureImageDescriptor(
                    FaceSentimentAnalyzerConst.SKILL_INPUTNAME_IMAGE,
                    "the input image onto which the sentiment analysis runs",
                    true, // isRequired (since this is an input, it is required to be bound before the evaluation occurs)
                    -2, // width divisible by 2
                    -2, // height divisible by 2
                    BitmapPixelFormat.Nv12,
                    BitmapAlphaMode.Ignore)
            );

            // Describe first output feature
            m_outputSkillDesc = new List<ISkillFeatureDescriptor>();
            m_outputSkillDesc.Add(
                new SkillFeatureTensorDescriptor(
                    FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACEBOUNDINGBOXES,
                    "a set of face bounding boxes in relative coordinates (left, top, right, bottom)",
                    false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                    new List<int>() { -1, 4 }, // tensor shape
                    SkillElementKind.Float)
                );

            // Describe second output feature
            m_outputSkillDesc.Add(
                new SkillFeatureTensorDescriptor(
                    FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACESENTIMENTSSCORES,
                    "a set of prediction scores for the supported sentiments of each face detected",
                    false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                    new List<int>() { -1, 8 }, // tensor shape
                    SkillElementKind.Float)
                );
        }

        /// <summary>
        /// Retrieves a list of supported ISkillExecutionDevice to run the skill logic on.
        /// </summary>
        /// <returns></returns>
        public IAsyncOperation<IReadOnlyList<ISkillExecutionDevice>> GetSupportedExecutionDevicesAsync()
        {
            return AsyncInfo.Run(async (token) =>
            {
                return await Task.Run(() =>
                {
                    var result = new List<ISkillExecutionDevice>();

                    // Add CPU as supported device
                    result.Add(SkillExecutionDeviceCPU.Create());

                    // Retrieve a list of DirectX devices available on the system and filter them by keeping only the ones that support DX12+ feature level
                    var devices = SkillExecutionDeviceDirectX.GetAvailableDirectXExecutionDevices();
                    var compatibleDevices = devices.Where((device) => (device as SkillExecutionDeviceDirectX).MaxSupportedFeatureLevel >= D3DFeatureLevelKind.D3D_FEATURE_LEVEL_12_0);
                    result.AddRange(compatibleDevices);

                    return result as IReadOnlyList<ISkillExecutionDevice>;
                });
            });
        }

        /// <summary>
        /// Factory method for instantiating and initializing the skill.
        /// Let the skill decide of the optimal or default ISkillExecutionDevice available to use.
        /// </summary>
        /// <returns></returns>
        public IAsyncOperation<ISkill> CreateSkillAsync()
        {
            return AsyncInfo.Run(async (token) =>
            {
                var supportedDevices = await GetSupportedExecutionDevicesAsync();
                ISkillExecutionDevice deviceToUse = supportedDevices.First();

                // Either use the first device returned (CPU) or the highest performing GPU
                int powerIndex = int.MaxValue;
                foreach (var device in supportedDevices)
                {
                    if (device.ExecutionDeviceKind == SkillExecutionDeviceKind.Gpu)
                    {
                        var directXDevice = device as SkillExecutionDeviceDirectX;
                        if (directXDevice.HighPerformanceIndex < powerIndex)
                        {
                            deviceToUse = device;
                            powerIndex = directXDevice.HighPerformanceIndex;
                        }
                    }
                }
                return await CreateSkillAsync(deviceToUse);
            });
        }

        /// <summary>
        /// Factory method for instantiating and initializing the skill.
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
        /// Returns the information concerning this skill such as Name, Description, etc.
        /// </summary>
        public SkillInformation Information { get; private set; }

        /// <summary>
        /// Returns a list of descriptors that correlate with each input SkillFeature.
        /// </summary>
        public IReadOnlyList<ISkillFeatureDescriptor> InputFeatureDescriptors => m_inputSkillDesc;

        /// <summary>
        /// Returns a set of metadata that may control the skill execution differently than by feeding an input 
        /// i.e. internal state, sub-process execution frequency, etc.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metadata => null;

        /// <summary>
        /// Returns a list of descriptors that correlate with each output SkillFeature.
        /// </summary>
        public IReadOnlyList<ISkillFeatureDescriptor> OutputFeatureDescriptors => m_outputSkillDesc;
    }
}
