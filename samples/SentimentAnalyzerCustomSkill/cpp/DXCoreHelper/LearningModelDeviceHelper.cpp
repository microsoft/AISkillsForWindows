#include "pch.h"
#include "LearningModelDeviceHelper.h"

using namespace winrt::Windows::AI::MachineLearning;

#define THROW_IF_FAILED(hr)                                                                                            \
    {                                                                                                                  \
        if (FAILED(hr))                                                                                                \
            throw winrt::hresult_error(hr);                                                                                   \
    }

namespace winrt::DXCoreHelper::implementation
{
    Windows::Foundation::Collections::IVectorView<LearningModelDevice> LearningModelDeviceHelper::EnumerateLearningModelDevices()
    {
        auto validAdapters = EnumerateDXCoreAdapters();
        auto validDevices = single_threaded_vector<LearningModelDevice>();

        for (auto&& kvPair : validAdapters)
        {
            com_ptr<IDXCoreAdapter> spAdapter = kvPair.second;
            validDevices.Append(CreateLearningModelDeviceFromAdapter(spAdapter.get()));
        }

        return validDevices.GetView();
    }

    LearningModelDevice LearningModelDeviceHelper::GetVpu()
    {
        auto validAdapters = EnumerateDXCoreAdapters();

        for (auto&& kvPair : validAdapters)
        {
            com_ptr<IDXCoreAdapter> spAdapter = kvPair.second;
            DXCoreHardwareID dxCoreHardwareID;
            THROW_IF_FAILED(spAdapter->GetHardwareID(&dxCoreHardwareID));
            if (dxCoreHardwareID.vendorId == 0x8086 && dxCoreHardwareID.deviceId == 0x6200) // VPU adapter
            {
                // For the developer preview, DXCore requires you to specifically choose the desired adapter;
                // In this case, the vendor and device IDs are for the Intel MyriadX VPU
                return CreateLearningModelDeviceFromAdapter(spAdapter.get());
            }
        }
    }

    std::unordered_map<int, com_ptr<IDXCoreAdapter>> LearningModelDeviceHelper::EnumerateDXCoreAdapters()
    {
        com_ptr<IDXCoreAdapterFactory_Internal> spFactory;
        THROW_IF_FAILED(DXCoreCreateAdapterFactory(IID_PPV_ARGS(spFactory.put())));

        com_ptr<IDXCoreAdapterList> spAdapterList;
        const GUID dxGUIDs[] = { DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE };

        THROW_IF_FAILED(spFactory->GetAdapterList(dxGUIDs, ARRAYSIZE(dxGUIDs), spAdapterList.put()));

        CHAR driverDescription[128];
        std::unordered_map<int, com_ptr<IDXCoreAdapter>> validAdapters;
        for (UINT i = 0; i < spAdapterList->GetAdapterCount(); i++)
        {
            com_ptr<IDXCoreAdapter> spAdapter;
            THROW_IF_FAILED(spAdapterList->GetItem(i, spAdapter.put()));
            // If the adapter is a software adapter then don't consider it
            bool isHardware;
            DXCoreHardwareID dxCoreHardwareID;
            THROW_IF_FAILED(spAdapter->QueryProperty(DXCoreProperty::IsHardware, sizeof(isHardware), &isHardware));
            THROW_IF_FAILED(spAdapter->GetHardwareID(&dxCoreHardwareID));
            if (isHardware && (dxCoreHardwareID.vendorId != 0x1414 || dxCoreHardwareID.deviceId != 0x8c))
            {
                // FIXME REMOVE
                THROW_IF_FAILED(spAdapter->QueryProperty(DXCoreProperty::DriverDescription, sizeof(driverDescription), driverDescription));
                printf("Index: %d, Description: %s\n", i, driverDescription);
                validAdapters[i] = spAdapter;
            }
        }

        return validAdapters;
    }

    LearningModelDevice LearningModelDeviceHelper::CreateLearningModelDeviceFromAdapter(IDXCoreAdapter* spAdapter)
    {
        IUnknown* pAdapter = spAdapter;
        com_ptr<IDXGIAdapter> spDxgiAdapter;
        D3D_FEATURE_LEVEL d3dFeatureLevel = D3D_FEATURE_LEVEL_1_0_CORE;
        D3D12_COMMAND_LIST_TYPE commandQueueType = D3D12_COMMAND_LIST_TYPE_COMPUTE;

        // Check if adapter selected has DXCORE_ADAPTER_ATTRIBUTE_D3D12_GRFX attribute selected. If so,
        // then GPU was selected that has D3D12 and D3D11 capabilities. It would be the most stable to
        // use DXGI to enumerate GPU and use D3D_FEATURE_LEVEL_11_0 so that image tensorization for
        // video frames would be able to happen on the GPU.
        if (spAdapter->IsDXAttributeSupported(DXCORE_ADAPTER_ATTRIBUTE_D3D12_GRFX))
        {
            d3dFeatureLevel = D3D_FEATURE_LEVEL::D3D_FEATURE_LEVEL_11_0;
            com_ptr<IDXGIFactory4> dxgiFactory4;
            THROW_IF_FAILED(CreateDXGIFactory2(0, __uuidof(IDXGIFactory4), dxgiFactory4.put_void()));

            // If DXGI factory creation was successful then get the IDXGIAdapter from the LUID acquired from the spAdapter
            LUID adapterLuid;
            THROW_IF_FAILED(spAdapter->GetLUID(&adapterLuid));
            THROW_IF_FAILED(dxgiFactory4->EnumAdapterByLuid(adapterLuid, __uuidof(IDXGIAdapter), spDxgiAdapter.put_void()));
            pAdapter = spDxgiAdapter.get();
        }
        else
        {
            // Need to enable experimental features to create D3D12 Device with adapter that has compute only capabilities.
            THROW_IF_FAILED(D3D12EnableExperimentalFeatures(1, &D3D12ComputeOnlyDevices, nullptr, 0));
        }

        // create D3D12Device
        com_ptr<ID3D12Device> d3d12Device;
        THROW_IF_FAILED(D3D12CreateDevice(pAdapter, d3dFeatureLevel, __uuidof(ID3D12Device), d3d12Device.put_void()));

        // create D3D12 command queue from device
        com_ptr<ID3D12CommandQueue> d3d12CommandQueue;
        D3D12_COMMAND_QUEUE_DESC commandQueueDesc = {};
        commandQueueDesc.Type = commandQueueType;
        THROW_IF_FAILED(d3d12Device->CreateCommandQueue(&commandQueueDesc, __uuidof(ID3D12CommandQueue), d3d12CommandQueue.put_void()));

        // create LearningModelDevice from command queue
        auto factory = get_activation_factory<LearningModelDevice, ILearningModelDeviceFactoryNative>();
        com_ptr<::IUnknown> spUnkLearningModelDevice;
        THROW_IF_FAILED(factory->CreateFromD3D12CommandQueue(d3d12CommandQueue.get(), spUnkLearningModelDevice.put()));
        return spUnkLearningModelDevice.as<LearningModelDevice>();
    }
}
