// Copyright (c) Microsoft Corporation. All rights reserved. 

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.AI.Skills.SkillInterface;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.FaceAnalysis;
using Windows.Storage;

namespace Contoso.FaceSentimentAnalyzer
{
    /// <summary>
    /// FaceSentimentAnalyzerSkill class.
    /// Contains the main execution logic of the skill, findind a face in an image then running an ML model to infer its sentiment
    /// Also acts as a factory for FaceSentimentAnalyzerBinding
    /// to obtain the ONNX model used in this skill, refer to https://github.com/onnx/models/tree/master/emotion_ferplus
    /// </summary>
    public sealed class FaceSentimentAnalyzerSkill : ISkill
    {
        // Face detection related members
        private FaceDetector m_faceDetector = null;

        // WinML related members
        private LearningModelSession m_winmlSession = null;

        /// <summary>
        /// Creates and initializes a FaceSentimentAnalyzerSkill instance
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        internal static IAsyncOperation<FaceSentimentAnalyzerSkill> CreateAsync(
            ISkillDescriptor descriptor,
            ISkillExecutionDevice device)
        {
            return AsyncInfo.Run(async (token) =>
            {
                // Create instance
                var skillInstance = new FaceSentimentAnalyzerSkill(descriptor, device);

                // Instantiate the FaceDetector
                skillInstance.m_faceDetector = await FaceDetector.CreateAsync();

                // Load WinML model
                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Contoso.FaceSentimentAnalyzer/{FaceSentimentAnalyzerConst.WINML_MODEL_FILENAME}"));
                var winmlModel = LearningModel.LoadFromFilePath(modelFile.Path);

                // Create WinML session
                skillInstance.m_winmlSession = new LearningModelSession(winmlModel, GetWinMLDevice(device));

                return skillInstance;
            });
        }

        /// <summary>
        /// FaceSentimentAnalyzerSkill constructor
        /// </summary>
        /// <param name="description"></param>
        /// <param name="device"></param>
        private FaceSentimentAnalyzerSkill(
            ISkillDescriptor description,
            ISkillExecutionDevice device)
        {
            SkillDescriptor = description;
            Device = device;
        }

        /// <summary>
        /// Factory method for instantiating FaceSentimentAnalyzerBinding
        /// </summary>
        /// <returns></returns>
        public IAsyncOperation<ISkillBinding> CreateSkillBindingAsync()
        {
            return AsyncInfo.Run((token) =>
            {
                var completedTask = new TaskCompletionSource<ISkillBinding>();
                completedTask.SetResult(new FaceSentimentAnalyzerBinding(SkillDescriptor, Device, m_winmlSession));
                return completedTask.Task;
            });
        }

        /// <summary>
        /// Runs the skill against a binding object, executing the skill logic on the associated input features and populating the output ones
        /// This skill proceeds in 2 steps: 
        /// 1) Run FaceDetector against the image and populate the face bound feature in the binding object
        /// 2) If a face was detected, proceeds with sentiment analysis of that portion fo the image using Windows ML then updating the score 
        /// of each possible sentiment returned as result
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public IAsyncAction EvaluateAsync(ISkillBinding binding)
        {
            FaceSentimentAnalyzerBinding bindingObj = binding as FaceSentimentAnalyzerBinding;
            if (bindingObj == null)
            {
                throw new ArgumentException("Invalid ISkillBinding parameter: This skill handles evaluation of FaceSentimentAnalyzerBinding instances only");
            }

            return AsyncInfo.Run(async (token) =>
            {
                // Retrieve input frame from the binding object
                VideoFrame inputFrame = (binding[FaceSentimentAnalyzerConst.SKILL_INPUTNAME_IMAGE].FeatureValue as SkillFeatureImageValue).VideoFrame;
                SoftwareBitmap softwareBitmapInput = inputFrame.SoftwareBitmap;

                // Retrieve a SoftwareBitmap to run face detection
                if (softwareBitmapInput == null)
                {
                    if (inputFrame.Direct3DSurface == null)
                    {
                        throw (new ArgumentNullException("An invalid input frame has been bound"));
                    }
                    softwareBitmapInput = await SoftwareBitmap.CreateCopyFromSurfaceAsync(inputFrame.Direct3DSurface);
                }

                // Run face detection and retrieve face detection result
                var faceDetectionResults = await m_faceDetector.DetectFacesAsync(softwareBitmapInput);

                // Retrieve face rectangle feature from the binding object
                var faceBoundingBoxesFeature = binding[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACEBOUNDINGBOXES];
                var faceBoundingBoxes = new List<float>();

                // Retrieve face sentiment scores feature from the binding object
                var faceSentimentsScoresFeature = binding[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACESENTIMENTSSCORES];
                var faceSentimentsScores = new List<float>();

                // If a face is found, update face rectangle feature
                if (faceDetectionResults.Count > 0)
                {
                    foreach (var faceDetectionResult in faceDetectionResults)
                    {
                        BitmapBounds faceBounds = faceDetectionResult.FaceBox;

                        // Retrieve the face bound and enlarge it by a factor of 1.5x to be sure to cover the whole facial area, while also ensuring clamping to frame dimensions
                        var additionalOffset = faceBounds.Width / 2;
                        faceBounds.X = Math.Max(0, faceBounds.X - additionalOffset);
                        faceBounds.Y = Math.Max(0, faceBounds.Y - additionalOffset);
                        faceBounds.Width = (uint)Math.Min(faceBounds.Width + 2 * additionalOffset, softwareBitmapInput.PixelWidth - faceBounds.X);
                        faceBounds.Height = (uint)Math.Min(faceBounds.Height + 2 * additionalOffset, softwareBitmapInput.PixelHeight - faceBounds.Y);

                        // Add the face bounding box
                        // note that values are in normalized coordinates between [0, 1] for ease of use
                        faceBoundingBoxes.Add((float)faceBounds.X / softwareBitmapInput.PixelWidth); // left
                        faceBoundingBoxes.Add((float)faceBounds.Y / softwareBitmapInput.PixelHeight); // top
                        faceBoundingBoxes.Add((float)(faceBounds.X + faceBounds.Width) / softwareBitmapInput.PixelWidth);// right
                        faceBoundingBoxes.Add((float)(faceBounds.Y + faceBounds.Height) / softwareBitmapInput.PixelHeight); // bottom


                        // Bind the WinML input frame with the adequate face bounds specified as metadata
                        bindingObj.m_winmlBinding.Bind(
                            FaceSentimentAnalyzerConst.WINML_MODEL_INPUTNAME, // WinML feature name
                            inputFrame, // VideoFrame
                            new PropertySet() // VideoFrame bounds
                            {
                            { "BitmapBounds",
                                PropertyValue.CreateUInt32Array(new uint[]{ faceBounds.X, faceBounds.Y, faceBounds.Width, faceBounds.Height })
                            }
                            });

                        // Run WinML evaluation
                        var winMLEvaluationResult = await m_winmlSession.EvaluateAsync(bindingObj.m_winmlBinding, "");
                        var winMLModelResult = (winMLEvaluationResult.Outputs[FaceSentimentAnalyzerConst.WINML_MODEL_OUTPUTNAME] as TensorFloat).GetAsVectorView();
                        AppendSoftMaxedInputs(winMLModelResult, ref faceSentimentsScores);
                    }
                }

                // Set the face bounding boxes SkillFeatureValue in the skill binding object
                // note that values are in normalized coordinates between [0, 1] for ease of use
                await faceBoundingBoxesFeature.SetFeatureValueAsync(faceBoundingBoxes);

                // Set the SkillFeatureValue in the skill binding object related to the face sentiment scores for each possible SentimentType
                // note that we SoftMax the output of WinML to give a score normalized between [0, 1] for ease of use
                await faceSentimentsScoresFeature.SetFeatureValueAsync(faceSentimentsScores);
            });
        }

        /// <summary>
        /// Returns the descriptor of this skill
        /// </summary>
        public ISkillDescriptor SkillDescriptor { get; private set; }

        /// <summary>
        /// Return the execution device with which this skill was initialized
        /// </summary>
        public ISkillExecutionDevice Device { get; private set; }

        /// <summary>
        /// Calculates SoftMax normalization over a set of data and append them to the output
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <returns></returns>
        private void AppendSoftMaxedInputs(IReadOnlyList<float> inputs, ref List<float> outputs)
        {
            float inputsExpSum = 0;
            int offset = outputs.Count;
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                outputs.Add((float)Math.Exp(input));
                inputsExpSum += outputs[offset + i];
            }
            inputsExpSum = inputsExpSum == 0 ? 1 : inputsExpSum;
            for (int i = 0; i < inputs.Count; i++)
            {
                outputs[offset + i] /= inputsExpSum;
            }
        }

        /// <summary>
        /// If possible, retrieves a WinML LearningModelDevice that corresponds to an ISkillExecutionDevice
        /// </summary>
        /// <param name="executionDevice"></param>
        /// <returns></returns>
        private static LearningModelDevice GetWinMLDevice(ISkillExecutionDevice executionDevice)
        {
            switch (executionDevice.ExecutionDeviceKind)
            {
                case SkillExecutionDeviceKind.Cpu:
                    return new LearningModelDevice(LearningModelDeviceKind.Cpu);

                case SkillExecutionDeviceKind.Gpu:
                    {
                        var gpuDevice = executionDevice as SkillExecutionDeviceDirectX;
                        return LearningModelDevice.CreateFromDirect3D11Device(gpuDevice.Direct3D11Device);
                    }

                default:
                    throw new ArgumentException("Passing unsupported SkillExecutionDeviceKind");
            }
        }
    }
}
