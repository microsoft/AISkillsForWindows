// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once
#include <winrt/Windows.Security.Cryptography.Core.h>
#include <rpc.h>
using namespace winrt::Windows::Storage;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Security::Cryptography::Core;

namespace ObfuscationToolset
{
    class ObfuscationHelper
    {
        IBuffer m_combinedKey;
        CryptographicKey GetCryptoKey();
    public:
        ObfuscationHelper(winrt::hstring key);
        IRandomAccessStreamReference GetDeObfuscatedStreamFromFile(StorageFile ObfuscatedModelFile);
        void PerformObfuscation(StorageFile ModelFile, StorageFile ObfuscatedModelFile);
    };
}