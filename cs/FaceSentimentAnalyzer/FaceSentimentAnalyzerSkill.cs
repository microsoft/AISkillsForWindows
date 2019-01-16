// Copyright (c) Microsoft Corporation. All rights reserved. 

//! \file FaceSentimentAnalyzerSkill.cs
//! \mainpage Windows Vision Skill Sample documentation
//!
//! \section summary_sec Summary
//!
//! # Sentiment analysis Windows Vision Skill sample
//! 
//! Provides an end-to-end sample to show how to write a Windows Vision Skill by extending the Microsoft.AI.Skills.SkillInterfacePreview base API.
//! This skill's implementation leverages the Windows built-in FaceDetector and Windows.AI.MachineLearning APIs along a model in ONNX format to identify a human face in an image and infer its sentiment.
//! This sample also contains a UWP application that ingests the skill's WindowsRuntimeComponent and put it to use against images.
//! 
//! 
//! Specifically, this sample shows how to:
//! 
//! 1. **Create and implement a skill decriptor class** inherited from ISkillDescriptor that provides information on the skill, provides a list of supported execution devices(CPU, GPU) and acts as a factory object for the skill.
//! 2. **Create and implement a skill class** inherited from ISkill that executes the skill logic and produces output given a set of input, and acts as a factory object for the skill binding.
//! 3. **Create and implement a skill binding class** inherited from VisionSkillBinding runtimeclass that contains input and output features as well as expose adapter to ease usage by developers.
//! 4. **Ingest a custom Windows Vision Skill** to execute againt images by binding VideoFrame instances and displaying binding outputs via adapter properties.
//! 
//! ## Related topics
//! **Reference**
//! [Using the Face Detection effect](https://docs.microsoft.com/en-us/uwp/api/Windows.Media.FaceAnalysis.FaceDetector)
//! [Using Windows ML](https://docs.microsoft.com/en-us/windows/ai/)
//! [Acquiring the sentiment analysis ONNX model](https://github.com/onnx/models/tree/master/emotion_ferplus)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.FaceAnalysis;
using Windows.Storage;

namespace FaceSentimentAnalyzer
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
                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///FaceSentimentAnalyzer/{FaceSentimentAnalyzerConst.WINML_MODEL_FILENAME}"));
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
        /// Run the skill against a binding object, executing the skill logic on the associated input features and populating the output ones
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
                var faceDetectionResult = await m_faceDetector.DetectFacesAsync(softwareBitmapInput);

                // Retrieve face rectangle feature from the binding object
                var faceRectangleFeature = binding[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACERECTANGLE];

                // Retrieve face sentiment scores feature from the binding object
                var faceSentimentScores = binding[FaceSentimentAnalyzerConst.SKILL_OUTPUTNAME_FACESENTIMENTSCORES];

                // If a face is found, update face rectangle feature
                if (faceDetectionResult.Count > 0)
                {
                    // Retrieve the face bound and enlarge it by a factor of 1.5x while also ensuring clamping to frame dimensions
                    BitmapBounds faceBound = faceDetectionResult[0].FaceBox;
                    var additionalOffset = faceBound.Width / 2;
                    faceBound.X = Math.Max(0, faceBound.X - additionalOffset);
                    faceBound.Y = Math.Max(0, faceBound.Y - additionalOffset);
                    faceBound.Width = (uint)Math.Min(faceBound.Width + 2 * additionalOffset, softwareBitmapInput.PixelWidth - faceBound.X);
                    faceBound.Height = (uint)Math.Min(faceBound.Height + 2 * additionalOffset, softwareBitmapInput.PixelHeight - faceBound.Y);

                    // Set the face rectangle SkillFeatureValue in the skill binding object
                    // note that values are in normalized coordinates between [0, 1] for ease of use
                    await faceRectangleFeature.SetFeatureValueAsync(
                        new List<float>()
                        {
                                (float)faceBound.X / softwareBitmapInput.PixelWidth, // left
                                (float)faceBound.Y / softwareBitmapInput.PixelHeight, // top
                                (float)(faceBound.X + faceBound.Width) / softwareBitmapInput.PixelWidth, // right
                                (float)(faceBound.Y + faceBound.Height) / softwareBitmapInput.PixelHeight // bottom
                        });

                    // Bind the WinML input frame with the adequate face bounds specified as metadata
                    bindingObj.m_winmlBinding.Bind(
                        FaceSentimentAnalyzerConst.WINML_MODEL_INPUTNAME, // WinML feature name
                        inputFrame, // VideoFrame
                        new PropertySet() // VideoFrame bounds
                        {
                            { "BitmapBounds",
                                PropertyValue.CreateUInt32Array(new uint[]{ faceBound.X, faceBound.Y, faceBound.Width, faceBound.Height })
                            }
                        });

                    // Run WinML evaluation
                    var winMLEvaluationResult = await m_winmlSession.EvaluateAsync(bindingObj.m_winmlBinding, "");
                    var winMLModelResult = (winMLEvaluationResult.Outputs[FaceSentimentAnalyzerConst.WINML_MODEL_OUTPUTNAME] as TensorFloat).GetAsVectorView();
                    var predictionScores = SoftMax(winMLModelResult);

                    // Set the SkillFeatureValue in the skill binding object related to the face sentiment scores for each possible SentimentType
                    // note that we SoftMax the output of WinML to give a score normalized between [0, 1] for ease of use
                    await faceSentimentScores.SetFeatureValueAsync(predictionScores);
                }
                else // if no face found, reset output SkillFeatureValues with 0s
                {
                    await faceRectangleFeature.SetFeatureValueAsync(FaceSentimentAnalyzerConst.ZeroFaceRectangleCoordinates);
                    await faceSentimentScores.SetFeatureValueAsync(FaceSentimentAnalyzerConst.ZeroFaceSentimentScores);
                }                    
            });
        }

        /// <summary>
        /// Return the descriptor of this skill
        /// </summary>
        public ISkillDescriptor SkillDescriptor { get; private set; }

        /// <summary>
        /// Return the execution device with which this skill was initialized
        /// </summary>
        public ISkillExecutionDevice Device { get; private set; }

        /// <summary>
        /// Calculate SoftMax normalization over a set of data
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        private List<float> SoftMax(IReadOnlyList<float> inputs)
        {
            List<float> inputsExp = new List<float>();
            float inputsExpSum = 0;
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                inputsExp.Add((float)Math.Exp(input));
                inputsExpSum += inputsExp[i];
            }
            inputsExpSum = inputsExpSum == 0 ? 1 : inputsExpSum;
            for (int i = 0; i < inputs.Count; i++)
            {
                inputsExp[i] /= inputsExpSum;
            }
            return inputsExp;
        }

        /// <summary>
        /// If possible, retrieve a WinML LearningModelDevice that corresponds to an ISkillExecutionDevice
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
                        var gpuDevice = executionDevice as SkillExecutionDeviceGPU;
                        return LearningModelDevice.CreateFromDirect3D11Device(gpuDevice.Direct3D11Device);
                    }

                default:
                    throw new ArgumentException("Passing unsupported SkillExecutionDeviceKind");
            }
        }
    }
}
