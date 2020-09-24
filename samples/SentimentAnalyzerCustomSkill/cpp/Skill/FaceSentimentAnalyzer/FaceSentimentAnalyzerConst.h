// Copyright (c) Microsoft Corporation. All rights reserved.

#pragma once

#include "winrt/base.h"
#include "winrt/Windows.Foundation.Collections.h"

/// <summary>
/// Set of contant values used throughout the skill
/// </summary>
namespace winrt::Contoso::FaceSentimentAnalyzer::implementation
{
    const winrt::hstring WINML_MODEL_FILENAME = L"emotion_ferplus.crypt";
    const hstring WINML_MODEL_INPUTNAME = L"Input3";
    const hstring WINML_MODEL_OUTPUTNAME = L"Plus692_Output_0";
    const hstring SKILL_INPUTNAME_IMAGE = L"InputImage";
    const hstring SKILL_OUTPUTNAME_FACEBOUNDINGBOXES = L"FaceBoundinGBoxes";
    const hstring SKILL_OUTPUTNAME_FACESENTIMENTSSCORES = L"FaceSentimentsScores";
}