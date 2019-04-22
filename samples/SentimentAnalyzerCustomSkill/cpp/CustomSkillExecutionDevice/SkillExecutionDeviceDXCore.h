// Copyright (C) Microsoft Corporation. All rights reserved.

#pragma once
#include "SkillExecutionDeviceDXCore.g.h"

namespace winrt::Contoso::CustomSkillExecutionDevice::implementation
{
    struct SkillExecutionDeviceDXCore : SkillExecutionDeviceDXCoreT<SkillExecutionDeviceDXCore>
    {
        // Non-WinRT-projected members, i.e. will not be available to consumers of this component
        SkillExecutionDeviceDXCore::SkillExecutionDeviceDXCore(IDXCoreAdapter* pAdapter);
        LUID AdapterLuid() const;

        // WinRT projected members, will be available to consumers of this Runtime Component,
        // i.e. these methods may be called by a consuming UWP app or another Runtime Component
        static Windows::Foundation::Collections::IVectorView<Contoso::CustomSkillExecutionDevice::SkillExecutionDeviceDXCore> GetAvailableHardwareExecutionDevices();

        Windows::AI::MachineLearning::LearningModelDevice WinMLDevice() const;
        Microsoft::AI::Skills::SkillInterfacePreview::SkillExecutionDeviceKind ExecutionDeviceKind() const;
        hstring Name() const;
        uint64_t AdapterId() const;
        uint64_t DedicatedVideoMemory() const;

    private:
        union Luid_uint64
        {
            uint64_t uint;
            LUID luid;
        };

        com_ptr<IDXCoreAdapter> m_spAdapter;
        hstring m_name;
        Microsoft::AI::Skills::SkillInterfacePreview::SkillExecutionDeviceKind m_executionDeviceKind;
        Windows::AI::MachineLearning::LearningModelDevice m_device;
        Luid_uint64 m_adapterLuid;
        uint64_t m_dedicatedVideoMemory;

    private:
        // Helper methods
        static hstring GetDriverDescriptionFromAdapter(IDXCoreAdapter* pAdapter);
        static Windows::AI::MachineLearning::LearningModelDevice GetLearningModelDeviceFromAdapter(IDXCoreAdapter* pAdapter);
    };
}
namespace winrt::Contoso::CustomSkillExecutionDevice::factory_implementation
{
    struct SkillExecutionDeviceDXCore : SkillExecutionDeviceDXCoreT<SkillExecutionDeviceDXCore, implementation::SkillExecutionDeviceDXCore>
    {
    };
}
