// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "ObfuscationHelper.h"

using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Security::Cryptography::Core;
using namespace winrt::Windows::Security::Cryptography;
namespace ObfuscationToolset
{
    //
    // ObfuscationHelper class constructor
    //
    ObfuscationHelper::ObfuscationHelper(winrt::hstring key)
    {
        // Convert from string to guid
        winrt::guid guidKey;
        UuidFromString((RPC_WSTR)(key.c_str()), (UUID*)&guidKey);

        // {688885AD-6073-49C0-A9B2-1A9C00F2798A}
        winrt::guid sharedKey = { 0x688885ad, 0x6073, 0x49c0, { 0xa9, 0xb2, 0x1a, 0x9c, 0x0, 0xf2, 0x79, 0x8a } };

        // Combine our shared key with the key passed as parameter to produce a unique key
        winrt::guid combinedKey;
        combinedKey.Data1 = sharedKey.Data1 & guidKey.Data1;
        combinedKey.Data2 = sharedKey.Data2 & guidKey.Data2;
        combinedKey.Data3 = sharedKey.Data3 & guidKey.Data3;
        for (int i = 0; i < sizeof(combinedKey.Data4); i++)
        {
            combinedKey.Data4[i] = sharedKey.Data4[i] & guidKey.Data4[i];
        }
        auto bufWriter = DataWriter();
        bufWriter.WriteGuid(combinedKey);
        m_combinedKey = bufWriter.DetachBuffer();
    }

    //
    // Create and return a symetric key
    //
    CryptographicKey ObfuscationHelper::GetCryptoKey()
    {
        SymmetricKeyAlgorithmProvider alg = SymmetricKeyAlgorithmProvider::OpenAlgorithm(SymmetricAlgorithmNames::AesCbcPkcs7());
        return alg.CreateSymmetricKey(m_combinedKey);
    }

    //
    // Retrieve a stream from an obfuscated file
    //
    IRandomAccessStreamReference ObfuscationHelper::GetDeObfuscatedStreamFromFile(StorageFile ObfuscatedFile)
    {
        auto outFileStream = InMemoryRandomAccessStream();

        auto inFileStream = ObfuscatedFile.OpenReadAsync().get();
        Buffer buf((uint32_t)(inFileStream.Size()));
        inFileStream.ReadAsync(buf, (uint32_t)(inFileStream.Size()), InputStreamOptions::None).get();
        inFileStream.Close();
        
        auto outbuf = CryptographicEngine::Decrypt(GetCryptoKey(), buf.as<IBuffer>(), m_combinedKey);
        outFileStream.WriteAsync(outbuf).get();
        outFileStream.FlushAsync().get();
        outFileStream.Seek(0);
        return RandomAccessStreamReference::CreateFromStream(outFileStream);
    }

    //
    // Perform obfuscation of a file and return an obfuscated file
    //
    void ObfuscationHelper::PerformObfuscation(StorageFile ModelFile, StorageFile ObfuscatedModelFile)
    {
        auto inFileStream = ModelFile.OpenReadAsync().get();
        Buffer buf((uint32_t)(inFileStream.Size()));
        inFileStream.ReadAsync(buf, (uint32_t)(inFileStream.Size()), InputStreamOptions::None).get();
        auto outbuf = CryptographicEngine::Encrypt(GetCryptoKey(), buf.as<IBuffer>(), m_combinedKey);

        auto outFileStream = ObfuscatedModelFile.OpenAsync(FileAccessMode::ReadWrite).get();
        outFileStream.WriteAsync(outbuf).get();
        outFileStream.FlushAsync().get();

        outFileStream.Close();
        inFileStream.Close();
    }
}
