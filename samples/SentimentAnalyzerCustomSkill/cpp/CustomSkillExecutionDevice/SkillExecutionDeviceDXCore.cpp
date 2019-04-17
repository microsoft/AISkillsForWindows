// Copyright (C) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "SkillExecutionDeviceDXCore.h"

namespace winrt::Contoso::CustomSkillExecutionDevice::implementation
{
    // TODO: Instead of always creating a LearningModelDevice, consider just caching the DirectX
    // device. Note that we previously, we cached the Direct3DDevice device, but that path fails
    // when using DXCore for device enumeration (ultimately, retrieving the DXGIAdapter fails
    // which blocks LearningModelDevice creation). Ideally we would accept either a D3D12 device
    // (from DXCore enumeration) or a Direct3D (DX11) device (from DXGI enumeration) and lazily
    // convert D3D12->Direct3DDevice and/or (DX device)->LearningModelDevice as needed
    SkillExecutionDeviceDXCore::SkillExecutionDeviceDXCore(IDXCoreAdapter* pAdapter) :
        m_device(nullptr),
        m_dedicatedVideoMemory(0)
    {
        m_spAdapter.attach(pAdapter);

        // Retrieve driver description (device name) from adapter
        m_name = GetDriverDescriptionFromAdapter(m_spAdapter.get());

        // Retrieve device kind from adapter
        m_executionDeviceKind = (m_spAdapter->IsDXAttributeSupported(DXCORE_ADAPTER_ATTRIBUTE_D3D12_GRFX)) ?
            Microsoft::AI::Skills::SkillInterfacePreview::SkillExecutionDeviceKind::Gpu :
            Microsoft::AI::Skills::SkillInterfacePreview::SkillExecutionDeviceKind::Vpu;

        // Retrieve WinML device from adapter
        m_device = GetLearningModelDeviceFromAdapter(m_spAdapter.get());

        // Retrieve dedicated video memory
        check_hresult(m_spAdapter->QueryProperty(
            DXCoreProperty::DedicatedVideoMemory,
            sizeof(m_dedicatedVideoMemory),
            &m_dedicatedVideoMemory
        ));

        // Retrieve adapter LUID
        check_hresult(m_spAdapter->GetLUID(&m_adapterLuid.luid));
    }

    LUID SkillExecutionDeviceDXCore::AdapterLuid() const
    {
        return m_adapterLuid.luid;
    }

    Windows::Foundation::Collections::IVectorView<Contoso::CustomSkillExecutionDevice::SkillExecutionDeviceDXCore> SkillExecutionDeviceDXCore::GetAvailableHardwareExecutionDevices()
    {
        auto results = single_threaded_vector<Contoso::CustomSkillExecutionDevice::SkillExecutionDeviceDXCore>();

        // Create DXCoreAdapterFactory
        winrt::com_ptr<IDXCoreAdapterFactory> factory;
        check_hresult(DXCoreCreateAdapterFactory(IID_PPV_ARGS(factory.put())));

        // Retrieve DXCoreAdapter list
        com_ptr<IDXCoreAdapterList> spAdapterList;
        const GUID dxGUIDs[] = { DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE };
        check_hresult(factory->GetAdapterList(dxGUIDs, ARRAYSIZE(dxGUIDs), spAdapterList.put()));

        // Step through devices and compile results
        for (UINT i = 0; i < spAdapterList->GetAdapterCount(); i++)
        {
            com_ptr<IDXCoreAdapter> spAdapter;
            check_hresult(spAdapterList->GetItem(i, spAdapter.put()));

            bool isHardware = false;
            check_hresult(spAdapter->QueryProperty(DXCoreProperty::IsHardware,
                sizeof(isHardware),
                &isHardware));

            if (isHardware)
            {
                auto executionDevice = make<SkillExecutionDeviceDXCore>(spAdapter.get());
                results.Append(executionDevice);
            }
        }

        return results.GetView();
    }

    Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice SkillExecutionDeviceDXCore::Direct3D11Device() const
    {
        return m_device.Direct3D11Device();
    }

    Windows::AI::MachineLearning::LearningModelDevice SkillExecutionDeviceDXCore::WinMLDevice() const
    {
        return m_device;
    }

    Microsoft::AI::Skills::SkillInterfacePreview::SkillExecutionDeviceKind SkillExecutionDeviceDXCore::ExecutionDeviceKind() const
    {
        return m_executionDeviceKind;
    }

    hstring SkillExecutionDeviceDXCore::Name() const
    {
        return m_name;
    }

    uint64_t SkillExecutionDeviceDXCore::AdapterId() const
    {
        return m_adapterLuid.uint;
    }

    uint64_t SkillExecutionDeviceDXCore::DedicatedVideoMemory() const
    {
        return m_dedicatedVideoMemory;
    }

    hstring SkillExecutionDeviceDXCore::GetDriverDescriptionFromAdapter(IDXCoreAdapter* pAdapter)
    {
        char descriptionBuffer[128];
        check_hresult(pAdapter->QueryProperty(
            DXCoreProperty::DriverDescription,
            sizeof(descriptionBuffer),
            descriptionBuffer
        ));
        WCHAR driverDescription[128];
        size_t bufferSizeUsed;
        mbstowcs_s(&bufferSizeUsed, driverDescription, descriptionBuffer, ARRAYSIZE(driverDescription));
        return hstring(driverDescription);
    }

    Windows::AI::MachineLearning::LearningModelDevice SkillExecutionDeviceDXCore::GetLearningModelDeviceFromAdapter(IDXCoreAdapter* pAdapter)
    {
        D3D_FEATURE_LEVEL d3dFeatureLevel = D3D_FEATURE_LEVEL_1_0_CORE;
        D3D12_COMMAND_LIST_TYPE commandQueueType = D3D12_COMMAND_LIST_TYPE_COMPUTE;

        // Need to enable experimental features to create D3D12 Device with adapter that has compute only capabilities.
        winrt::check_hresult(D3D12EnableExperimentalFeatures(1, &D3D12ComputeOnlyDevices, nullptr, 0));

        // create D3D12Device
        winrt::com_ptr<ID3D12Device> spD3D12Device;
        winrt::check_hresult(D3D12CreateDevice(pAdapter, d3dFeatureLevel, __uuidof(ID3D12Device), spD3D12Device.put_void()));

        // create D3D12 command queue from device
        winrt::com_ptr<ID3D12CommandQueue> spD3D12CommandQueue;
        D3D12_COMMAND_QUEUE_DESC commandQueueDesc = {};
        commandQueueDesc.Type = commandQueueType;
        winrt::check_hresult(spD3D12Device->CreateCommandQueue(&commandQueueDesc, __uuidof(ID3D12CommandQueue), spD3D12CommandQueue.put_void()));

        // create LearningModelDevice from command queue
        auto factory = winrt::get_activation_factory<Windows::AI::MachineLearning::LearningModelDevice, ILearningModelDeviceFactoryNative>();
        winrt::com_ptr<::IUnknown> spUnkLearningModelDevice;
        winrt::check_hresult(factory->CreateFromD3D12CommandQueue(spD3D12CommandQueue.get(), spUnkLearningModelDevice.put()));
        return spUnkLearningModelDevice.as<Windows::AI::MachineLearning::LearningModelDevice>();
    }
}