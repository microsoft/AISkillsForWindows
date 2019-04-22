// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "FaceSentimentAnalyzerConst.h"
#include "FaceSentimentAnalyzerDescriptor.h"
#include "FaceSentimentAnalyzerSkill.h"
#include "winrt/Microsoft.AI.Skills.DXCoreExecutionDevice.h"

using namespace winrt::Microsoft::AI::Skills::SkillInterfacePreview;
using namespace winrt::Microsoft::AI::Skills::DXCoreExecutionDevice;

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    // {678BD455-4190-45D3-B5DA-41543283C092}
    const guid FaceSentimentAnalyzerId = guid(0x678bd455, 0x4190, 0x45d3, { 0xb5, 0xda, 0x41, 0x54, 0x32, 0x83, 0xc0, 0x92 });

    //
    // FaceSentimentAnalyzerDescriptor constructor
    //
    FaceSentimentAnalyzerDescriptor::FaceSentimentAnalyzerDescriptor()
    {
        m_version = SkillVersion::Create(
            0,  // major version
            1,  // minor version
            L"Contoso Developer", // Author name 
            L"Contoso Publishing" // Publisher name
        );

        auto inputSkillDesc = single_threaded_vector<ISkillFeatureDescriptor>();
        auto outputSkillDesc = single_threaded_vector<ISkillFeatureDescriptor>();

        // Describe input feature
        inputSkillDesc.Append(
            SkillFeatureImageDescriptor(
                SKILL_INPUTNAME_IMAGE,
                L"the input image onto which the sentiment analysis runs",
                true, // isRequired (since this is an input, it is required to be bound before the evaluation occurs)
                -1, // width
                -1, // height
                -1, // maxDimension
                Windows::Graphics::Imaging::BitmapPixelFormat::Nv12,
                Windows::Graphics::Imaging::BitmapAlphaMode::Ignore)
        );

        // Describe first output feature
        outputSkillDesc.Append(
            SkillFeatureTensorDescriptor(
                SKILL_OUTPUTNAME_FACERECTANGLE,
                L"a face bounding box in relative coordinates (left, top, right, bottom)",
                false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                single_threaded_vector<int64_t>({ 4 }).GetView(), // tensor shape
                SkillElementKind::Float)
        );

        // Describe second output feature
        outputSkillDesc.Append(
            SkillFeatureTensorDescriptor(
                SKILL_OUTPUTNAME_FACESENTIMENTSCORES,
                L"the prediction score for each class",
                false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                single_threaded_vector<int64_t>({ 1, 8 }).GetView(), // tensor shape
                SkillElementKind::Float)
        );

        m_inputSkillDesc = inputSkillDesc.GetView();
        m_outputSkillDesc = outputSkillDesc.GetView();
    }

    //
    // Retrieves a list of supported ISkillExecutionDevice to run the skill logic on
    //
    Windows::Foundation::IAsyncOperation<Windows::Foundation::Collections::IVectorView<ISkillExecutionDevice>> FaceSentimentAnalyzerDescriptor::GetSupportedExecutionDevicesAsync()
    {
        m_devices = single_threaded_vector<ISkillExecutionDevice>();
        m_devices.Append(SkillExecutionDeviceCPU::Create());
        auto devices = SkillExecutionDeviceDXCore::GetAvailableHardwareExecutionDevices();
        for (auto iter : devices)
        {
            m_devices.Append(iter);
        }
        co_await resume_background();
        return m_devices.GetView();
    }

    //
    // Factory method for instantiating and initializing the skill using the optimal execution device available
    //
    Windows::Foundation::IAsyncOperation<ISkill> FaceSentimentAnalyzerDescriptor::CreateSkillAsync()
    {
        auto supportedDevices = GetSupportedExecutionDevicesAsync().get();
        ISkillExecutionDevice deviceToUse = supportedDevices.First().Current();

        // Use the first GPU or VPU device if available, otherwise default to CPU
        for (auto device : supportedDevices)
        {
            if (device.ExecutionDeviceKind() == SkillExecutionDeviceKind::Gpu
                || device.ExecutionDeviceKind() == SkillExecutionDeviceKind::Vpu)
            {
                deviceToUse = device;
                break;
            }
        }
        return CreateSkillAsync(deviceToUse);
    }

    //
    // Factory method for instantiating and initializing the skill
    //
    Windows::Foundation::IAsyncOperation<ISkill> FaceSentimentAnalyzerDescriptor::CreateSkillAsync(ISkillExecutionDevice const executionDevice)
    {
        co_await resume_background();
        auto desc = this->operator winrt::Windows::Foundation::IInspectable().as<ISkillDescriptor>();
        auto skill = FaceSentimentAnalyzerSkill::CreateAsync(desc, executionDevice).get();
        return skill;
    }

    winrt::guid FaceSentimentAnalyzerDescriptor::Id()
    {
        return FaceSentimentAnalyzerId;
    }

    hstring FaceSentimentAnalyzerDescriptor::Name()
    {
        return L"FaceSentimentAnalyzer";
    }
    
    hstring FaceSentimentAnalyzerDescriptor::Description()
    {
        return L"Finds a face in the image and infers its predominant sentiment from a set of 8 possible labels";
    }

    SkillVersion FaceSentimentAnalyzerDescriptor::Version()
    {
        return m_version;
    }

    Windows::Foundation::Collections::IVectorView<ISkillFeatureDescriptor> FaceSentimentAnalyzerDescriptor::InputFeatureDescriptors()
    {
        return m_inputSkillDesc;
    }

    Windows::Foundation::Collections::IVectorView<ISkillFeatureDescriptor> FaceSentimentAnalyzerDescriptor::OutputFeatureDescriptors()
    {
        return m_outputSkillDesc;
    }

    Windows::Foundation::Collections::IMapView<hstring, hstring> FaceSentimentAnalyzerDescriptor::Metadata()
    {
        return single_threaded_map<hstring, hstring>().GetView();
    }

    
}
