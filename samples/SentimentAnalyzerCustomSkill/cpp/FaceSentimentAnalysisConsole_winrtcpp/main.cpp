// Copyright (c) Microsoft Corporation. All rights reserved.
#include "App.h"
int main()
{
    try 
    {
        class App ap;
        ap.AppMain();
    }
    catch (hresult_error const&ex)
    {
        std::cout << "Error:" << std::hex << ex.code() << ":" << ex.message().c_str();
    }
    
}
