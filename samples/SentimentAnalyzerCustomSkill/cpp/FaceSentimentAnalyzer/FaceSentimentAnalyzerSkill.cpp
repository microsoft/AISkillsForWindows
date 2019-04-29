// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "FaceSentimentAnalyzerSkill.h"
#include "FaceSentimentAnalyzerBinding.h"
#include "FaceSentimentAnalyzerConst.h"
#include "winrt/DeobfuscationHelper.h"
#include <ppltasks.h>
#include <math.h>

using namespace winrt::Microsoft::AI::Skills::SkillInterfacePreview;
using namespace winrt::Windows::Media::FaceAnalysis;
using namespace winrt::Windows::AI::MachineLearning;
using namespace winrt::Windows::Graphics::Imaging;
using namespace winrt::Windows::Media;

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    //
    // If possible, retrieves a WinML LearningModelDevice that corresponds to an ISkillExecutionDevice
    //
    LearningModelDevice GetWinMLDevice(ISkillExecutionDevice executionDevice)
    {
        switch (executionDevice.ExecutionDeviceKind())
        {
        case SkillExecutionDeviceKind::Cpu:
            return LearningModelDevice(LearningModelDeviceKind::Cpu);
        case SkillExecutionDeviceKind::Gpu:
        case SkillExecutionDeviceKind::Vpu:
        {
            auto dxDevice = executionDevice.as<DXCoreExtension::SkillExecutionDeviceDXCore>();
            return dxDevice.WinMLDevice();
        }
        default:
            throw new hresult_invalid_argument(L"Passing unsupported SkillExecutionDeviceKind");
        }
    }

    //
    // Calculates SoftMax normalization over a set of data
    //
    Windows::Foundation::Collections::IVector<float> SoftMax(Windows::Foundation::Collections::IVectorView<float> inputs)
    {
        Windows::Foundation::Collections::IVector<float> inputsExp = single_threaded_vector<float>();
        float inputsExpSum = 0;
        for (uint32_t i = 0; i < inputs.Size(); i++)
        {
            auto input = inputs.GetAt(i);
            inputsExp.Append((float)exp(input));
            inputsExpSum += inputsExp.GetAt(i);
        }
        inputsExpSum = inputsExpSum == 0 ? 1 : inputsExpSum;
        for (uint32_t i = 0; i < inputs.Size(); i++)
        {
            inputsExp.SetAt(i, inputsExp.GetAt(i) / inputsExpSum);
        }
        return inputsExp;
    }

    //
    // Creates and initializes a FaceSentimentAnalyzerSkill instance
    //
    Windows::Foundation::IAsyncOperation<winrt::Contoso::FaceSentimentAnalyzer::FaceSentimentAnalyzerSkill> FaceSentimentAnalyzerSkill::CreateAsync(
        ISkillDescriptor descriptor,
        ISkillExecutionDevice device)
    {
        co_await resume_background();

        // Instantiate the FaceDetector
        auto faceDetector = FaceDetector::CreateAsync().get();

        // Load WinML model
        auto modelFile = Windows::Storage::StorageFile::GetFileFromApplicationUriAsync(Windows::Foundation::Uri(L"ms-appx:///Contoso.FaceSentimentAnalyzer/" + WINML_MODEL_FILENAME)).get();
        
        // Deobfuscate model file and retrieve LearningModel instance
        LearningModel learningModel = winrt::DeobfuscationHelper::Deobfuscator::DeobfuscateModelAsync(modelFile, descriptor.Id()).get();

        // Create WinML session
        auto winmlSession = LearningModelSession(learningModel, GetWinMLDevice(device));

        return make<FaceSentimentAnalyzerSkill>(descriptor, device, faceDetector, winmlSession);
    }

    //
    // Factory method for instantiating FaceSentimentAnalyzerBinding
    //
    Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterfacePreview::ISkillBinding> FaceSentimentAnalyzerSkill::CreateSkillBindingAsync()
    {
        co_await resume_background();
        auto binding = make<FaceSentimentAnalyzerBinding>(m_skillDescriptor, m_device, m_winmlSession);
        return binding;
    }

    //
    // Runs the skill against a binding object, executing the skill logic on the associated input features and populating the output ones
    // This skill proceeds in 2 steps: 
    // 1) Run FaceDetector against the image and populate the face bound feature in the binding object
    // 2) If a face was detected, proceeds with sentiment analysis of that portion fo the image using Windows ML then updating the score 
    // of each possible sentiment returned as result
    //
    Windows::Foundation::IAsyncAction FaceSentimentAnalyzerSkill::EvaluateAsync(Microsoft::AI::Skills::SkillInterfacePreview::ISkillBinding const binding)
    {
        auto bindingObj = binding.try_as<FaceSentimentAnalyzerBinding>();
        if (bindingObj == nullptr)
        {
            throw new hresult_invalid_argument(L"Invalid ISkillBinding parameter: This skill handles evaluation of FaceSentimentAnalyzerBinding instances only");
        }

        co_await resume_background();

        // Retrieve input frame from the binding object
        VideoFrame inputFrame = binding.Lookup(SKILL_INPUTNAME_IMAGE).FeatureValue().as<SkillFeatureImageValue>().VideoFrame();
        SoftwareBitmap softwareBitmapInput = inputFrame.SoftwareBitmap();

        // Retrieve a SoftwareBitmap to run face detection
        if (softwareBitmapInput == nullptr)
        {
            if (inputFrame.Direct3DSurface() == nullptr)
            {
                throw (new hresult_invalid_argument(L"An invalid input frame has been bound"));
            }
            softwareBitmapInput = SoftwareBitmap::CreateCopyFromSurfaceAsync(inputFrame.Direct3DSurface()).get();
        }

        // Run face detection and retrieve face detection result
        auto faceDetectionResult = m_faceDetector.DetectFacesAsync(softwareBitmapInput).get();

        // Retrieve face rectangle feature from the binding object
        auto faceRectangleFeature = binding.Lookup(SKILL_OUTPUTNAME_FACERECTANGLE);

        // Retrieve face sentiment scores feature from the binding object
        auto faceSentimentScores = binding.Lookup(SKILL_OUTPUTNAME_FACESENTIMENTSCORES);

        // If a face is found, update face rectangle feature
        if (faceDetectionResult.Size() > 0)
        {
            // Retrieve the face bound and enlarge it by a factor of 1.5x while also ensuring clamping to frame dimensions
            BitmapBounds faceBound = faceDetectionResult.GetAt(0).FaceBox();
            auto additionalOffset = faceBound.Width / 2;
            faceBound.X = max(0, faceBound.X - additionalOffset);
            faceBound.Y = max(0, faceBound.Y - additionalOffset);
            faceBound.Width = (uint32_t)min(faceBound.Width + 2 * additionalOffset, softwareBitmapInput.PixelWidth() - faceBound.X);
            faceBound.Height = (uint32_t)min(faceBound.Height + 2 * additionalOffset, softwareBitmapInput.PixelHeight() - faceBound.Y);

            // Set the face rectangle SkillFeatureValue in the skill binding object
            // note that values are in normalized coordinates between [0, 1] for ease of use
            faceRectangleFeature.SetFeatureValueAsync(
                single_threaded_vector<float>(
                {
                    (float)faceBound.X / softwareBitmapInput.PixelWidth(), // left
                    (float)faceBound.Y / softwareBitmapInput.PixelHeight(), // top
                    (float)(faceBound.X + faceBound.Width) / softwareBitmapInput.PixelWidth(), // right
                    (float)(faceBound.Y + faceBound.Height) / softwareBitmapInput.PixelHeight() // bottom
                }
            )).get();

            // Bind the WinML input frame with the adequate face bounds specified as metadata
            auto propSet = Windows::Foundation::Collections::PropertySet();
            propSet.Insert(L"BitmapBounds", 
                Windows::Foundation::PropertyValue::CreateUInt32Array({ faceBound.X, faceBound.Y, faceBound.Width, faceBound.Height }));
            bindingObj->m_winmlBinding.Bind(
                WINML_MODEL_INPUTNAME, // WinML feature name
                inputFrame, // VideoFrame
                propSet);

            // Run WinML evaluation
            auto winMLEvaluationResult = m_winmlSession.EvaluateAsync(bindingObj->m_winmlBinding, L"").get();
            WINRT_ASSERT(winMLEvaluationResult.Succeeded());
            auto winMLModelResult = winMLEvaluationResult.Outputs().Lookup(WINML_MODEL_OUTPUTNAME).as<TensorFloat>().GetAsVectorView();
            auto predictionScores = SoftMax(winMLModelResult);

            // Set the SkillFeatureValue in the skill binding object related to the face sentiment scores for each possible SentimentType
            // note that we SoftMax the output of WinML to give a score normalized between [0, 1] for ease of use
            faceSentimentScores.SetFeatureValueAsync(predictionScores).get();
        }
        else // if no face found, reset output SkillFeatureValues with 0s
        {
            faceRectangleFeature.SetFeatureValueAsync(ZeroFaceRectangleCoordinates).get();
            faceSentimentScores.SetFeatureValueAsync(ZeroFaceSentimentScores).get();
        }
    }
}
