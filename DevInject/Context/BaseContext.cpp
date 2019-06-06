#include "stdafx.h"
#include "Context/BaseContext.h"
#include "Pipe.h"

BaseContext::BaseContext()
{
}

BaseContext::~BaseContext()
{
}

void BaseContext::Initialize()
{
}

void BaseContext::Dispose()
{
}

struct FindOwnerProcessInfo
{
    HANDLE disposeEvent;
    Pipe& ownerPipe;
    DWORD processId;
};

static BOOL CALLBACK FindOwnerProcessWindow(HWND hwnd, LPARAM lp)
{
    FindOwnerProcessInfo* info = reinterpret_cast<FindOwnerProcessInfo*>(lp);
    if (!info->processId)
    {
        DWORD hwndProcessId;
        if (::GetWindowThreadProcessId(hwnd, &hwndProcessId))
        {
            HANDLE hwndProcess = ::OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, hwndProcessId);
            if (hwndProcess)
            {
                wchar_t path[MAX_PATH];
                if (::GetProcessImageFileName(hwndProcess, path, _countof(path)))
                {
                    const wchar_t* ownerSuffix = L"\\DevPrompt.exe";
                    const size_t suffixLen = std::wcslen(ownerSuffix);

                    const wchar_t* ownerSuffix2 = L"\\DevPromptNetCore.exe";
                    const size_t suffixLen2 = std::wcslen(ownerSuffix2);

                    size_t len = std::wcslen(path);
                    if ((len >= suffixLen && !_wcsicmp(ownerSuffix, path + (len - suffixLen))) ||
                        (len >= suffixLen2 && !_wcsicmp(ownerSuffix2, path + (len - suffixLen2))))
                    {
                        info->ownerPipe = Pipe::Connect(hwndProcess, info->disposeEvent);
                        if (info->ownerPipe)
                        {
                            info->processId = hwndProcessId;
                        }
                    }
                }

                ::CloseHandle(hwndProcess);
            }
        }
    }

    return TRUE;
}

HANDLE BaseContext::OpenOwnerProcess(HANDLE disposeEvent, Pipe& ownerPipe)
{
    assert(disposeEvent && !ownerPipe);

    FindOwnerProcessInfo info{ disposeEvent, ownerPipe, 0 };
    ::EnumDesktopWindows(nullptr, ::FindOwnerProcessWindow, reinterpret_cast<LPARAM>(&info));

    return (info.processId && info.processId != ::GetCurrentProcessId())
        ? ::OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE, FALSE, info.processId)
        : nullptr;
}
