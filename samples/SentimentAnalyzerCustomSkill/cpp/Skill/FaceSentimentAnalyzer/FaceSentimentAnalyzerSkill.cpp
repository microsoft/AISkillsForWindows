// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "FaceSentimentAnalyzerSkill.h"
#include "FaceSentimentAnalyzerBinding.h"
#include "FaceSentimentAnalyzerConst.h"
#include "winrt/DeobfuscationHelper.h"
#include <ppltasks.h>
#include <math.h>

using namespace winrt::Microsoft::AI::Skills::SkillInterface;
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
        {
            auto gpuDevice = executionDevice.as<SkillExecutionDeviceDirectX>();
            return LearningModelDevice::CreateFromDirect3D11Device(gpuDevice.Direct3D11Device());
        }

        default:
            throw new hresult_invalid_argument(L"Passing unsupported SkillExecutionDeviceKind");
        }
    }

    //
    // Calculates SoftMax normalization over a set of input data and append them to the output
    //
    void AppendSoftMaxedInputs(
        Windows::Foundation::Collections::IVectorView<float> inputs,
        Windows::Foundation::Collections::IVector<float>& outputs)
    {
        float inputsExpSum = 0;
        int offset = outputs.Size();
        for (uint32_t i = 0; i < inputs.Size(); i++)
        {
            auto input = inputs.GetAt(i);
            outputs.Append((float)exp(input));
            inputsExpSum += outputs.GetAt(offset + i);
        }
        inputsExpSum = inputsExpSum == 0 ? 1 : inputsExpSum;
        for (uint32_t i = 0; i < inputs.Size(); i++)
        {
            outputs.SetAt(offset + i, outputs.GetAt(offset + i) / inputsExpSum);
        }
    }

    // helper function to determine if the skill is being called from a UWP app container or not.
    bool IsUWPContainer()
    {
        HANDLE hProcessToken = INVALID_HANDLE_VALUE;
        HANDLE hProcess;

        hProcess = GetCurrentProcess();
        if (!OpenProcessToken(hProcess, TOKEN_QUERY, &hProcessToken))
        {
            throw winrt::hresult(HRESULT_FROM_WIN32(GetLastError()));
        }
        BOOL bIsAppContainer = false;
        DWORD dwLength;
        if (!GetTokenInformation(hProcessToken, TokenIsAppContainer, &bIsAppContainer, sizeof(bIsAppContainer), &dwLength))
        {
            // if we were denied token information we are definitely not in an app container.
            bIsAppContainer = false;
        }

        return bIsAppContainer;
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
        
        winrt::Windows::Storage::StorageFile modelFile = nullptr;
        if (IsUWPContainer())
        {
            modelFile = Windows::Storage::StorageFile::GetFileFromApplicationUriAsync(Windows::Foundation::Uri(L"ms-appx:///Contoso.FaceSentimentAnalyzer/" + WINML_MODEL_FILENAME)).get();
        }
        else
        {
            WCHAR DllPath[MAX_PATH] = { 0 };
            GetModuleFileName(NULL, DllPath, _countof(DllPath));
            auto file = Windows::Storage::StorageFile::GetFileFromPathAsync(DllPath).get();
            auto folder = file.GetParentAsync().get();
            modelFile = folder.GetFileAsync(WINML_MODEL_FILENAME).get();
        }
        // Deobfuscate model file and retrieve LearningModel instance
        LearningModel learningModel = winrt::DeobfuscationHelper::Deobfuscator::DeobfuscateModelAsync(modelFile, descriptor.Information().Id()).get();

        // Create WinML session
        auto winmlSession = LearningModelSession(learningModel, GetWinMLDevice(device));

        return make< FaceSentimentAnalyzerSkill>(descriptor, device, faceDetector, winmlSession);
    }

    //
    // Factory method for instantiating FaceSentimentAnalyzerBinding
    //
    Windows::Foundation::IAsyncOperation<Microsoft::AI::Skills::SkillInterface::ISkillBinding> FaceSentimentAnalyzerSkill::CreateSkillBindingAsync()
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
    Windows::Foundation::IAsyncAction FaceSentimentAnalyzerSkill::EvaluateAsync(Microsoft::AI::Skills::SkillInterface::ISkillBinding const binding)
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
        auto faceDetectionResults = m_faceDetector.DetectFacesAsync(softwareBitmapInput).get();

        // Retrieve face rectangle feature from the binding object
        auto faceBoundingBoxesFeature = binding.Lookup(SKILL_OUTPUTNAME_FACEBOUNDINGBOXES);
        auto faceBoundingBoxes = single_threaded_vector<float>();

        // Retrieve face sentiment scores feature from the binding object
        auto faceSentimentsScoresFeature = binding.Lookup(SKILL_OUTPUTNAME_FACESENTIMENTSSCORES);
        auto faceSentimentsScores = single_threaded_vector<float>();

        // If a face is found, update face rectangle feature
        if (faceDetectionResults.Size() > 0)
        {
            for (auto&& faceDetectionResult : faceDetectionResults)
            {
                BitmapBounds faceBounds = faceDetectionResult.FaceBox();

                // Retrieve the face bound and enlarge it by a factor of 1.5x to be sure to cover the whole facial area, while also ensuring clamping to frame dimensions
                int additionalOffset = faceBounds.Width / 2;
                faceBounds.X = max(0, (int)faceBounds.X - additionalOffset);
                faceBounds.Y = max(0, (int)faceBounds.Y - additionalOffset);
                faceBounds.Width = (uint32_t)min(faceBounds.Width + 2 * additionalOffset, softwareBitmapInput.PixelWidth() - faceBounds.X);
                faceBounds.Height = (uint32_t)min(faceBounds.Height + 2 * additionalOffset, softwareBitmapInput.PixelHeight() - faceBounds.Y);

                // Add the face bounding box
                // note that values are in normalized coordinates between [0, 1] for ease of use
                faceBoundingBoxes.Append((float)faceBounds.X / softwareBitmapInput.PixelWidth()); // left
                faceBoundingBoxes.Append((float)faceBounds.Y / softwareBitmapInput.PixelHeight()); // top
                faceBoundingBoxes.Append((float)(faceBounds.X + faceBounds.Width) / softwareBitmapInput.PixelWidth());// right
                faceBoundingBoxes.Append((float)(faceBounds.Y + faceBounds.Height) / softwareBitmapInput.PixelHeight()); // bottom

                // Bind the WinML input frame with the adequate face bounds specified as metadata
                auto propSet = Windows::Foundation::Collections::PropertySet();
                propSet.Insert(L"BitmapBounds",
                    Windows::Foundation::PropertyValue::CreateUInt32Array({ faceBounds.X, faceBounds.Y, faceBounds.Width, faceBounds.Height }));
                bindingObj->m_winmlBinding.Bind(
                    WINML_MODEL_INPUTNAME, // WinML feature name
                    inputFrame, // VideoFrame
                    propSet);

                // Run WinML evaluation
                auto winMLEvaluationResult = m_winmlSession.EvaluateAsync(bindingObj->m_winmlBinding, L"").get();
                auto winMLModelResult = winMLEvaluationResult.Outputs().Lookup(WINML_MODEL_OUTPUTNAME).as<TensorFloat>().GetAsVectorView();

                AppendSoftMaxedInputs(winMLModelResult, faceSentimentsScores);
            }
        }

        // Set the face bounding boxes SkillFeatureValue in the skill binding object
        // note that values are in normalized coordinates between [0, 1] for ease of use
        faceBoundingBoxesFeature.SetFeatureValueAsync(faceBoundingBoxes).get();

        // Set the SkillFeatureValue in the skill binding object related to the face sentiment scores for each possible SentimentType
        // note that we SoftMax the output of WinML to give a score normalized between [0, 1] for ease of use
        faceSentimentsScoresFeature.SetFeatureValueAsync(faceSentimentsScores).get();
    }
}
