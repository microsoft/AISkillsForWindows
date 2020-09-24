// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "FaceSentimentAnalyzerConst.h"
#include "FaceSentimentAnalyzerDescriptor.h"
#include "FaceSentimentAnalyzerSkill.h"

using namespace winrt::Microsoft::AI::Skills::SkillInterface;

namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    // {678BD455-4190-45D3-B5DA-41543283C092}
    const guid FaceSentimentAnalyzerId = guid(0x678bd455, 0x4190, 0x45d3, { 0xb5, 0xda, 0x41, 0x54, 0x32, 0x83, 0xc0, 0x92 });

    //
    // FaceSentimentAnalyzerDescriptor constructor
    //
    FaceSentimentAnalyzerDescriptor::FaceSentimentAnalyzerDescriptor()
    {
        m_information = SkillInformation::Create(
            L"FaceSentimentAnalyzer", // Name
            L"Finds the faces in the image and infers their predominant sentiments from a set of 8 possible labels", // Description
            FaceSentimentAnalyzerId, // Id
            { 0, 0, 0, 9 }, // Version
            L"Contoso Developer", // Author
            L"Contoso Publishing" // Publisher
        );

        auto inputSkillDesc = single_threaded_vector<ISkillFeatureDescriptor>();
        auto outputSkillDesc = single_threaded_vector<ISkillFeatureDescriptor>();

        // Describe input feature
        inputSkillDesc.Append(
            SkillFeatureImageDescriptor(
                SKILL_INPUTNAME_IMAGE,
                L"the input image onto which the sentiment analysis runs",
                true, // isRequired (since this is an input, it is required to be bound before the evaluation occurs)
                -2, // width divisible by 2
                -2, // height divisble by 2
                Windows::Graphics::Imaging::BitmapPixelFormat::Nv12,
                Windows::Graphics::Imaging::BitmapAlphaMode::Ignore)
        );

        // Describe first output feature
        outputSkillDesc.Append(
            SkillFeatureTensorDescriptor(
                SKILL_OUTPUTNAME_FACEBOUNDINGBOXES,
                L"a set of face bounding boxes in relative coordinates (left, top, right, bottom)",
                false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                single_threaded_vector<int>({ -1, 4 }).GetView(), // tensor shape divisible by 4
                SkillElementKind::Float)
        );

        // Describe second output feature
        outputSkillDesc.Append(
            SkillFeatureTensorDescriptor(
                SKILL_OUTPUTNAME_FACESENTIMENTSSCORES,
                L"a set of prediction scores for the supported sentiments of each face detected",
                false, // isRequired (since this is an output, it automatically get populated after the evaluation occurs)
                single_threaded_vector<int>({ -1, 8 }).GetView(), // tensor shape divisible by 8
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
        
        auto dxDevices = SkillExecutionDeviceDXHelper::GetAvailableDXExecutionDevices();
        for (auto iter : dxDevices)
        {
            auto dxgiDevice = iter.try_as<SkillExecutionDeviceDirectX>();

            // Expose only D3D12 devices with support for D3D11 feature level or above to leverage WinML
            if (dxgiDevice
                && (dxgiDevice.MaxSupportedFeatureLevel() >= D3DFeatureLevelKind::D3D_FEATURE_LEVEL_11_0)
                && dxgiDevice.IsD3D12Supported())
            {
                m_devices.Append(iter);
            }
            else if (!dxgiDevice)
            {
                auto dxCoreDevice = iter.try_as<SkillExecutionDeviceDXCore>();
                if (dxCoreDevice
                    && (dxCoreDevice.MaxSupportedFeatureLevel() >= D3DFeatureLevelKind::D3D_FEATURE_LEVEL_1_0_CORE))
                {
                    m_devices.Append(iter);
                }
            }
        }

        co_await resume_background();
        return m_devices.GetView();
    }

    //
    // Factory method for instantiating and initializing the skill using the optimal execution device available
    //
    Windows::Foundation::IAsyncOperation<ISkill> FaceSentimentAnalyzerDescriptor::CreateSkillAsync()
    {
        co_await resume_background();
        auto supportedDevices = co_await GetSupportedExecutionDevicesAsync();
        ISkillExecutionDevice deviceToUse = supportedDevices.First().Current();

        // Either use the first device returned (CPU) or the highest performing GPU
        int perfIndex = INT32_MAX;
        for (auto device : supportedDevices)
        {
            if (device.ExecutionDeviceKind() == SkillExecutionDeviceKind::Gpu)
            {
                auto dxgiDevice = device.try_as<SkillExecutionDeviceDirectX>();
                if (dxgiDevice && (dxgiDevice.HighPerformanceIndex() < perfIndex))
                {
                    deviceToUse = device;
                    perfIndex = dxgiDevice.HighPerformanceIndex();
                }
                else if (!dxgiDevice)
                {
                    auto dxcoreDevice = device.try_as<SkillExecutionDeviceDXCore>();
                    deviceToUse = device;
                    break; // for dxcore we do not have powerindex, so just select first GPU.
                }
            }
        }

        auto result = CreateSkillAsync(deviceToUse).get();
        return result;
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

    SkillInformation FaceSentimentAnalyzerDescriptor::Information()
    {
        return m_information;
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
