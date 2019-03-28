#include "stdafx.h"
#include "../DevInject/Inject.h"

int __stdcall wWinMain(_In_ HINSTANCE instance, _In_opt_ HINSTANCE prevInstance, _In_ LPWSTR commandLine, _In_ int windowShow)
{
    HANDLE process = nullptr;
    DWORD processId = 0;
    int charCount = 0;
    
    if (swscanf_s(commandLine, L"%u %u%n", reinterpret_cast<unsigned int*>(&processId), reinterpret_cast<unsigned int*>(&process), &charCount) == 2 &&
        charCount == static_cast<int>(std::wcslen(commandLine)) &&
        process && processId)
    {
        if (::GetProcessId(process) == processId && DevInject::InjectDll(process, process, false))
        {
            return 0;
        }
    }

    assert(false);
    return 1;
}
