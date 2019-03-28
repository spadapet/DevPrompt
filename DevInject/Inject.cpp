#include "stdafx.h"
#include "Inject.h"
#include "Main.h"

static const bool IsMyProcess64 = (PLATFORM_BITS == 64);

static bool InjectDllSameBitness(HANDLE process, HANDLE stopEvent)
{
    HMODULE remoteModule = nullptr;
    wchar_t dllBuffer[MAX_PATH];
    ::GetModuleFileName(DevInject::GetModule(), dllBuffer, _countof(dllBuffer));

    std::wstring dllPath = dllBuffer;
    size_t dllPathSize = (dllPath.size() + 1) * sizeof(wchar_t);

    wchar_t* dllPathRemote = reinterpret_cast<wchar_t*>(::VirtualAllocEx(process, nullptr, dllPathSize, MEM_COMMIT, PAGE_READWRITE));
    if (dllPathRemote)
    {
        if (::WriteProcessMemory(process, dllPathRemote, dllPath.c_str(), dllPathSize, nullptr))
        {
            HMODULE kernelModule = ::GetModuleHandle(L"Kernel32");
            LPTHREAD_START_ROUTINE loadLibrary = kernelModule ? reinterpret_cast<LPTHREAD_START_ROUTINE>(::GetProcAddress(kernelModule, "LoadLibraryW")) : nullptr;
            HANDLE remoteLoadLibrary = loadLibrary ? ::CreateRemoteThread(process, nullptr, 0, loadLibrary, dllPathRemote, 0, nullptr) : nullptr;

            if (remoteLoadLibrary)
            {
                std::array<HANDLE, 3> handles = { remoteLoadLibrary, stopEvent, process };
                if (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
                {
                    if (::GetExitCodeThread(remoteLoadLibrary, reinterpret_cast<DWORD*>(&remoteModule)))
                    {
                        // Should be injected now...
                    }
                }

                ::CloseHandle(remoteLoadLibrary);
            }
        }

        BOOL freed = ::VirtualFreeEx(process, dllPathRemote, 0, MEM_RELEASE);
        assert(freed);
    }

    assert(remoteModule);
    return remoteModule != nullptr;
}

// Use a helper process of opposite bitness to do the injection
static bool InjectDllOppositeBitness(_In_ HANDLE process, HANDLE stopEvent)
{
    // Handle must be transferable to a 32bit process
    assert((reinterpret_cast<size_t>(process) & 0xFFFFFFFF) == reinterpret_cast<size_t>(process));

    wchar_t dllBuffer[MAX_PATH];
    ::GetModuleFileName(DevInject::GetModule(), dllBuffer, _countof(dllBuffer));

    std::wstring exePath = dllBuffer;
    size_t slash = exePath.rfind('\\');
    if (slash == std::wstring::npos)
    {
        assert(false);
        return false;
    }

    exePath.erase(slash + 1);
    exePath += L"DevInjector";
    exePath += ::IsMyProcess64 ? L"32" : L"64";
    exePath += L".exe";

    std::wstringstream commandLineBuffer;
    commandLineBuffer << L"\"" << exePath << L"\" " << ::GetProcessId(process) << L" " << reinterpret_cast<size_t>(process);
    std::wstring commandLine = commandLineBuffer.str();

    STARTUPINFO si{};
    si.cb = sizeof(si);

    PROCESS_INFORMATION pi;
    DWORD flags = CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT;
    bool status = false;

    if (::CreateProcess(nullptr, &commandLine[0], nullptr, nullptr, TRUE, flags, nullptr, nullptr, &si, &pi))
    {
        ::ResumeThread(pi.hThread);
        ::WaitForSingleObject(pi.hProcess, INFINITE);

        DWORD exitCode;
        if (::GetExitCodeProcess(pi.hProcess, &exitCode))
        {
            status = (exitCode == 0);
        }

        ::CloseHandle(pi.hThread);
        ::CloseHandle(pi.hProcess);
    }

    return status;
}

bool DevInject::InjectDll(HANDLE process, HANDLE stopEvent, bool allowDifferentBitness)
{
    bool isOs64 = ::IsMyProcess64;
    if (!isOs64)
    {
        // Might be 32bit process on 64bit OS
        BOOL isWow64;
        isOs64 = ::IsWow64Process(::GetCurrentProcess(), &isWow64) && isWow64;
    }

    bool isOtherProcess64 = isOs64;
    if (isOtherProcess64)
    {
        // Might be 32bit process on 64bit OS
        BOOL isWow64;
        isOtherProcess64 = ::IsWow64Process(process, &isWow64) && !isWow64;
    }

    if (::IsMyProcess64 == isOtherProcess64)
    {
        return ::InjectDllSameBitness(process, stopEvent);
    }
    else if (allowDifferentBitness)
    {
        return ::InjectDllOppositeBitness(process, stopEvent);
    }

    return false;
}
