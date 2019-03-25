// Copyright (c) Microsoft Corporation. All rights reserved.
#pragma once

#include "Deobfuscator.g.h"

namespace winrt::DeobfuscationHelper::implementation
{
    struct Deobfuscator : DeobfuscatorT<Deobfuscator>
    {
        Deobfuscator() = delete;
        static Windows::Foundation::IAsyncOperation<Windows::AI::MachineLearning::LearningModel> DeobfuscateModelAsync(Windows::Storage::StorageFile const& encryptedFile, winrt::guid const& key);
    };
} // namespace winrt::ObfuscatorHelper::factory_implementation

namespace winrt::DeobfuscationHelper::factory_implementation
{
    struct Deobfuscator : DeobfuscatorT<Deobfuscator, implementation::Deobfuscator>
    {
    };
}