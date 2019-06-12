// Copyright (c) Microsoft Corporation. All rights reserved.
#pragma once

#include <Windows.h>
#include <iostream>

namespace WindowsVersionHelper
{
    //
    // Helper method to check the Windows version at runtime
    //
    static HRESULT EqualOrAboveWindows10Version(WORD versionNumber)
    {
        BOOL bSuccess = FALSE;
        HRESULT hr = S_OK;
        DWORD dwHandle;
        char *pData = nullptr;
        VS_FIXEDFILEINFO *pVer = nullptr;

        UINT dwSize;
        DWORD dwLen = GetFileVersionInfoSize(L"kernel32.dll", &dwHandle);
        if (dwLen)
        {
            pData = new char[dwLen];
            if (GetFileVersionInfo(L"kernel32.dll", dwHandle, dwLen, pData))
            {
                bSuccess = VerQueryValue(pData, L"\\", (LPVOID*)&pVer, &dwSize);
            }
        }

        if (!bSuccess)
        {
            std::cerr << "Failed to obtain current Windows version.";
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            if ((HIWORD(pVer->dwProductVersionMS) < 10) || (HIWORD(pVer->dwProductVersionLS) < versionNumber))
            {
                std::cout << "Current windows version is:";
                std::cout << HIWORD(pVer->dwProductVersionMS) << '.'
                    << LOWORD(pVer->dwProductVersionMS) << '.'
                    << HIWORD(pVer->dwProductVersionLS) << std::endl;
                std::cerr << "This application will work only on windows version 10.0." << versionNumber << ".x or newer." << std::endl;
                hr = HRESULT_FROM_WIN32(ERROR_OLD_WIN_VERSION);
            }
        }

        if (pData != nullptr)
        {
            delete[] pData;
        }
        return hr;
    }
};