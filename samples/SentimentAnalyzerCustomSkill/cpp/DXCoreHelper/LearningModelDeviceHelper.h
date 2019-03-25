#pragma once

#include "LearningModelDeviceHelper.g.h"

namespace winrt::DXCoreHelper::implementation
{
    MIDL_INTERFACE("e4212a94-d660-480e-82b3-006e050a44c0")
        IDXCoreAdapterFactory_Internal : public IDXCoreAdapterFactory{};

    struct LearningModelDeviceHelper
    {
        LearningModelDeviceHelper() = delete;

        static Windows::Foundation::Collections::IVectorView<Windows::AI::MachineLearning::LearningModelDevice> EnumerateLearningModelDevices();
        static Windows::AI::MachineLearning::LearningModelDevice GetVpu();

    private:
        static std::unordered_map<int, com_ptr<IDXCoreAdapter>> EnumerateDXCoreAdapters();
        static Windows::AI::MachineLearning::LearningModelDevice CreateLearningModelDeviceFromAdapter(IDXCoreAdapter* spAdapter);
    };
}

namespace winrt::DXCoreHelper::factory_implementation
{
    struct LearningModelDeviceHelper : LearningModelDeviceHelperT<LearningModelDeviceHelper, implementation::LearningModelDeviceHelper>
    {
    };
}
