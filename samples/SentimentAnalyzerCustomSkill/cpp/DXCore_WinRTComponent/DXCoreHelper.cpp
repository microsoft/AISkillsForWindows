#include "pch.h"
#include "DXCoreHelper.h"

using namespace winrt;
using namespace Windows::AI::MachineLearning;

namespace winrt::DXCore_WinRTComponent::implementation
{
	DXCoreHelper::DXCoreHelper()
	{
		// This IID is private and will break in 19H2.
		// Eventually replace with: com_ptr<IDXCoreAdapterFactory> spFactory;
		MIDL_INTERFACE("e4212a94-d660-480e-82b3-006e050a44c0") IDXCoreAdapterFactory_Internal : public IDXCoreAdapterFactory{};

		com_ptr<IDXCoreAdapterFactory_Internal> spFactory;
		check_hresult(DXCoreCreateAdapterFactory(IID_PPV_ARGS(spFactory.put())));
		spFactory.as(_factory);
	}

	///<summary>
	/// Uses the experimental DXCore API to select an MCDM adapter and create a
	/// WinML LearningModelDevice from it. Hardcoded to target the Intel MyriadX VPU; returns nullptr 
	/// if no VPU is found.
	///</summary>
	Windows::AI::MachineLearning::LearningModelDevice DXCoreHelper::GetVpuDeviceFromDXCore()
	{
		com_ptr<IDXCoreAdapterList> spAdapterList;
		const GUID dxGUIDs[] = { DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE };

		check_hresult(_factory->GetAdapterList(dxGUIDs, ARRAYSIZE(dxGUIDs), spAdapterList.put()));

		com_ptr<IDXCoreAdapter> vpuAdapter;
		for (UINT i = 0; i < spAdapterList->GetAdapterCount(); i++)
		{
			com_ptr<IDXCoreAdapter> spAdapter;
			check_hresult(spAdapterList->GetItem(i, spAdapter.put()));

			DXCoreHardwareID dxCoreHardwareID;
			check_hresult(spAdapter->GetHardwareID(&dxCoreHardwareID));

			if (dxCoreHardwareID.vendorId == 0x8086 && dxCoreHardwareID.deviceId == 0x6200) // VPU adapter
			{
				// For the developer preview, DXCore requires you to specifically choose the desired adapter;
				// In this case, the vendor and device IDs are for the Intel MyriadX VPU
				vpuAdapter = spAdapter;
			}
		}

		LearningModelDevice device = nullptr;
		if (vpuAdapter != nullptr)
		{
			device = GetLearningModelDeviceFromAdapter(vpuAdapter.get());
		}

		return device;
	}

	///<summary>
	/// Uses the experimental DXCore API to select an hardware adapter (GPU or VPU) and create a
	/// WinML LearningModelDevice from it. Uses the first valid hardware adapter found; if there are none
	/// returns nullptr.
	///</summary>
	winrt::Windows::AI::MachineLearning::LearningModelDevice DXCoreHelper::GetHardwareDeviceFromDXCore()
	{
		com_ptr<IDXCoreAdapterList> spAdapterList;
		const GUID dxGUIDs[] = { DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE };

		check_hresult(_factory->GetAdapterList(dxGUIDs, ARRAYSIZE(dxGUIDs), spAdapterList.put()));

		com_ptr<IDXCoreAdapter> hwAdapter;
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
				hwAdapter = spAdapter;
				break;
			}
		}

		LearningModelDevice device = nullptr;
		if (hwAdapter != nullptr)
		{
			device = GetLearningModelDeviceFromAdapter(hwAdapter.get());
		}

		return device;
	}

	winrt::Windows::Foundation::Collections::IVectorView<winrt::Windows::AI::MachineLearning::LearningModelDevice> DXCoreHelper::GetAvailableDevices()
	{
		com_ptr<IDXCoreAdapterList> spAdapterList;
		const GUID dxGUIDs[] = { DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE };

		check_hresult(_factory->GetAdapterList(dxGUIDs, ARRAYSIZE(dxGUIDs), spAdapterList.put()));

		auto devices = single_threaded_vector<winrt::Windows::AI::MachineLearning::LearningModelDevice>();
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
				LearningModelDevice device = GetLearningModelDeviceFromAdapter(spAdapter.get());
				devices.Append(device);
			}
		}

		return devices.GetView();
	}

	LearningModelDevice DXCoreHelper::GetLearningModelDeviceFromAdapter(IDXCoreAdapter* adapter)
	{
		D3D_FEATURE_LEVEL d3dFeatureLevel = D3D_FEATURE_LEVEL_1_0_CORE;
		D3D12_COMMAND_LIST_TYPE commandQueueType = D3D12_COMMAND_LIST_TYPE_COMPUTE;

		// Need to enable experimental features to create D3D12 Device with adapter that has compute only capabilities.
		check_hresult(D3D12EnableExperimentalFeatures(1, &D3D12ComputeOnlyDevices, nullptr, 0));

		// create D3D12Device
		com_ptr<ID3D12Device> d3d12Device;
		check_hresult(D3D12CreateDevice(adapter, d3dFeatureLevel, __uuidof(ID3D12Device), d3d12Device.put_void()));

		// create D3D12 command queue from device
		com_ptr<ID3D12CommandQueue> d3d12CommandQueue;
		D3D12_COMMAND_QUEUE_DESC commandQueueDesc = {};
		commandQueueDesc.Type = commandQueueType;
		check_hresult(d3d12Device->CreateCommandQueue(&commandQueueDesc, __uuidof(ID3D12CommandQueue), d3d12CommandQueue.put_void()));

		// create LearningModelDevice from command queue
		auto factory = get_activation_factory<LearningModelDevice, ILearningModelDeviceFactoryNative>();
		com_ptr<::IUnknown> spUnkLearningModelDevice;
		check_hresult(factory->CreateFromD3D12CommandQueue(d3d12CommandQueue.get(), spUnkLearningModelDevice.put()));
		return spUnkLearningModelDevice.as<LearningModelDevice>();
	}
}
