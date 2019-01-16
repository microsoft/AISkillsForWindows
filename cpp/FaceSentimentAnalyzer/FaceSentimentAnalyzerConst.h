// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "winrt/base.h"
#include "winrt/Windows.Foundation.Collections.h"

namespace winrt::FaceSentimentAnalyzer::implementation
{
    const winrt::hstring WINML_MODEL_FILENAME = L"emotion_ferplus.onnx";
    const hstring WINML_MODEL_INPUTNAME = L"Input3";
    const hstring WINML_MODEL_OUTPUTNAME = L"Plus692_Output_0";
    const hstring SKILL_INPUTNAME_IMAGE = L"InputImage";
    const hstring SKILL_OUTPUTNAME_FACERECTANGLE = L"faceRectangle";
    const hstring SKILL_OUTPUTNAME_FACESENTIMENTSCORES = L"faceSentimentScores";
    const winrt::Windows::Foundation::Collections::IVectorView<float> ZeroFaceRectangleCoordinates = single_threaded_vector<float>({ 0.0f, 0.0f, 0.0f, 0.0f }).GetView();
    const winrt::Windows::Foundation::Collections::IVectorView<float> ZeroFaceSentimentScores = single_threaded_vector<float>({ 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }).GetView();
}