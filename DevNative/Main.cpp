#include "stdafx.h"
#include "Interop/AppInterop.h"

static HINSTANCE instance = nullptr;

extern "C" __declspec(dllexport) void CreateApp(IAppHost* host, IApp** app)
{
    *app = new AppInterop(host, ::instance);
    (*app)->AddRef();
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        ::instance = module;
    }

    return TRUE;
}
