#pragma once

#ifdef DEVINJECT_EXPORTS
#define DEV_INJECT_API __declspec(dllexport)
#else
#define DEV_INJECT_API __declspec(dllimport)
#endif

namespace DevInject
{
    DEV_INJECT_API HMODULE InjectDll(HANDLE process, HANDLE stopEvent);
}
