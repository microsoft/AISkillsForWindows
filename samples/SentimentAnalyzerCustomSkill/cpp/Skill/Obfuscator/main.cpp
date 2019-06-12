// Copyright (c) Microsoft Corporation. All rights reserved.
// main.cpp : Defines the entry point for the console application.
//

#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Security::Cryptography;
using namespace ObfuscationToolset;

int wmain(int argc, wchar_t* argv[])
{
    init_apartment();
    if (argc < 5)
    {
        printf("Usage: Obfuscator.exe InputFileNameWithPath OutputFilePathOnly OutputFileNameOnly GuidKeyWithoutBraces");
        return -1;
    }
    auto inFileName = hstring(argv[1]);
    auto outFilePath = hstring(argv[2]);
    auto outFileName = hstring(argv[3]);
    auto strkey = hstring(argv[4]);

    printf("\nAttempting to perform obfuscation using:\n inFileName = %ls\n outFilePath = %ls\n outFileName = %ls\n strkey = %ls\n", 
        inFileName.c_str(), 
        outFilePath.c_str(),
        outFileName.c_str(),
        strkey.c_str());

    try
    {
        auto inputFile = StorageFile::GetFileFromPathAsync(inFileName).get();
        auto outputfolder = StorageFolder::GetFolderFromPathAsync(outFilePath).get();
        auto outputFile = outputfolder.CreateFileAsync(outFileName, CreationCollisionOption::ReplaceExisting).get();
        { 
            //scope limit the key and obfuscation objects for faster destruction
            ObfuscationHelper objObfuscation(strkey);
            objObfuscation.PerformObfuscation(inputFile, outputFile);
        }
        //verification
        {
            ObfuscationHelper objObfuscation(strkey);
            auto decodedStrmRef = objObfuscation.GetDeObfuscatedStreamFromFile(outputFile);
            auto decodedStrm = decodedStrmRef.OpenReadAsync().get();
            auto buf = Buffer((uint32_t)decodedStrm.Size());
            decodedStrm.ReadAsync(buf, (uint32_t)decodedStrm.Size(), InputStreamOptions::None).get();

            auto inputStrm = inputFile.OpenReadAsync().get();
            auto buf1 = Buffer((uint32_t)inputStrm.Size());
            if (CryptographicBuffer::Compare(buf, buf1))
            {
                return -1;
            }
        }
    }
    catch (hresult_error const& e)
    {
        printf("\nError during model encryption: %x::%ls\n", (int)e.code(), e.message().c_str());
    }
}
