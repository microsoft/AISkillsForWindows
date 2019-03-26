#pragma once

#include "DXCoreHelper.g.h"

namespace winrt::DXCore_WinRTComponent::implementation
{
    struct DXCoreHelper : DXCoreHelperT<DXCoreHelper>
    {
		DXCoreHelper();

		winrt::Windows::AI::MachineLearning::LearningModelDevice GetVpuDeviceFromDXCore();
		winrt::Windows::AI::MachineLearning::LearningModelDevice GetHardwareDeviceFromDXCore();
		winrt::Windows::Foundation::Collections::IVectorView<winrt::Windows::AI::MachineLearning::LearningModelDevice> GetAvailableDevices();

	private:
		winrt::Windows::AI::MachineLearning::LearningModelDevice GetLearningModelDeviceFromAdapter(IDXCoreAdapter* adapter);

		winrt::com_ptr<IDXCoreAdapterFactory> _factory;
    };
}

namespace winrt::DXCore_WinRTComponent::factory_implementation
{
    struct DXCoreHelper : DXCoreHelperT<DXCoreHelper, implementation::DXCoreHelper>
    {
    };
}
