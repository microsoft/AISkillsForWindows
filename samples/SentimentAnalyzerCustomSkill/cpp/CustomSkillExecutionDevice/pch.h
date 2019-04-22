// Copyright (C) Microsoft Corporation. All rights reserved.

#pragma once

// TODO: DXCoreCreateAdapterFactory in dxcore.h is unavailable for a UWP app, potentially this entry point is using the wrong
// WINAPI_PARTITION() check to determine OS eligibility.
// As a workaround, explicitly behave as if this is a desktop app which does allow the API to be used.
#undef WINAPI_FAMILY
#define WINAPI_FAMILY WINAPI_FAMILY_DESKTOP_APP
#include <initguid.h>
#include <dxcore.h>
#include <windows.ai.machinelearning.native.h>

//#include <Unknwn.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.AI.MachineLearning.h>
//#include <winrt/Windows.Graphics.DirectX.Direct3D11.h>
#include "winrt/Microsoft.AI.Skills.SkillInterfacePreview.h"