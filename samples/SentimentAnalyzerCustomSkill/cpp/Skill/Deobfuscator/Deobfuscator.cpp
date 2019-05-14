// Copyright (c) Microsoft Corporation. All rights reserved.

#include "pch.h"
#include "Deobfuscator.h"
#include "..\Obfuscator\ObfuscationHelper.h"

namespace winrt::DeobfuscationHelper::implementation
{
    //
    // Deobfuscate an ONNX model file and return a WinML model
    //
    Windows::Foundation::IAsyncOperation<Windows::AI::MachineLearning::LearningModel> Deobfuscator::DeobfuscateModelAsync(
        Windows::Storage::StorageFile const& encryptedFile, 
        winrt::guid const& key)
    {
        // convert guid to string
        WCHAR* wszUuid = NULL;
        UuidToString((UUID*)&key, ((RPC_WSTR*)&wszUuid));
        hstring guidStr = hstring(wszUuid);

        auto file = encryptedFile;

        co_await resume_background();

        // Perform deobfuscation and retrieve a stream
        ObfuscationToolset::ObfuscationHelper objObfuscation(guidStr);
        auto deObfuscatedStreamRef = objObfuscation.GetDeObfuscatedStreamFromFile(file);

        // Instantiate the LearningModel from the stream and return it
        auto learningModel = Windows::AI::MachineLearning::LearningModel::LoadFromStreamAsync(deObfuscatedStreamRef).get();

        return learningModel;
    }
}
