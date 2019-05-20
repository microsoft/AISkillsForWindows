// Copyright (c) Microsoft Corporation. All rights reserved.
#include <Windows.h>
#include "App.h"

HRESULT CheckVersion()
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
        std::cout << "Failed to obtain current Windows version.";
        hr = HRESULT_FROM_WIN32(GetLastError());
    }
    else
    {
        if ((HIWORD(pVer->dwProductVersionMS) < 10)
            || (HIWORD(pVer->dwProductVersionLS) < 18362)
            )
        {
            std::cout << "Current windows version is:";
            std::cout << HIWORD(pVer->dwProductVersionMS) << '.'
                << LOWORD(pVer->dwProductVersionMS) << '.'
                << HIWORD(pVer->dwProductVersionLS) << std::endl;
            std::cout << "This application will work only on windows verison 10.0.18362 or newer." << std::endl;
            hr = E_ABORT;
        }
    }

    if (pData != nullptr)
    {
        delete(pData);
    }
    return hr;
}

int main()
{
    try 
    {
        HRESULT hr = CheckVersion();
        if (FAILED(hr))
        {
            throw_hresult(hr);
        }
        App ap;
        ap.AppMain();
    }
    catch (hresult_error const&ex)
    {
        std::cout << "Error:" << std::hex << ex.code() << ":" << ex.message().c_str();
    }
    
}
