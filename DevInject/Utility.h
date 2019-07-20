#pragma once

#include "Api.h"

namespace DevInject
{
    DEV_INJECT_API std::wstring GetModuleFileName(HMODULE handle);
    DEV_INJECT_API bool InjectDll(HANDLE process, HANDLE stopEvent, bool allowDifferentBitness);
    DEV_INJECT_API void SetDebuggerThreadName(const std::wstring& name, DWORD threadId = 0);
    DEV_INJECT_API UINT GetDetachMessage();
    UINT GetAttachedMessage();

    void CheckConsoleWindowSize(bool visibleOnly);
    void BeginDetach(HWND waitForWindowToRespond = nullptr);
}
