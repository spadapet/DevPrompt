#include "stdafx.h"
#include "CommandHandler.h"
#include "Main.h"
#include "Pipe.h"

static HMODULE module = nullptr;
static HANDLE disposeEvent = nullptr;
static HANDLE ownerProcess = nullptr;
static HANDLE pipeServerThread = nullptr;
static HANDLE watchdogThread = nullptr;
static HANDLE findMainWindowThread = nullptr;
static std::mutex ownerPipeMutex;
static Pipe ownerPipe;

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        DevInject::Initialize(module);
        break;

    case DLL_PROCESS_DETACH:
        ::module = nullptr;
        break;

    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }

    return TRUE;
}

static void SendToOwner(const Message& message)
{
    std::scoped_lock<std::mutex> lock(::ownerPipeMutex);
    if (::ownerPipe)
    {
        ::ownerPipe.Send(message);
    }
}

static bool NotifyOwnerOfPipe(const Pipe& pipe)
{
    Message message(PIPE_COMMAND_PIPE_CREATED);

    wchar_t buffer[MAX_PATH];
    if (!pipe || !::GetFileInformationByHandleEx(pipe, FileNameInfo, &buffer, sizeof(buffer)))
    {
        return false;
    }

    FILE_NAME_INFO* nameInfo = reinterpret_cast<FILE_NAME_INFO*>(&buffer);
    message.SetValue(PIPE_PROPERTY_VALUE, std::wstring(nameInfo->FileName, nameInfo->FileNameLength / sizeof(wchar_t)));

    ::SendToOwner(message);
    return true;
}

// A thread that listens for commands coming from the owner process, and responds to them
static DWORD __stdcall PipeServerThread(void*)
{
    Pipe pipe = Pipe::Create(::ownerProcess, ::disposeEvent);

    if (::NotifyOwnerOfPipe(pipe) && pipe.WaitForClient())
    {
        pipe.RunServer(DevInject::CommandHandler);
    }

    return 0;
}

static void NotifyOwnerOfChanges(std::wstring& oldTitle, std::wstring& oldEnvironment)
{
    if (!::ownerPipe)
    {
        std::scoped_lock<std::mutex> lock(::ownerPipeMutex);
        if (!::ownerPipe)
        {
            return;
        }
    }

    HWND hwnd = ::GetConsoleWindow();
    if (hwnd)
    {
        wchar_t title[1024];
        if (!::GetWindowText(hwnd, title, _countof(title)))
        {
            title[0] = '\0';
        }

        if (oldTitle != title)
        {
            oldTitle = title;

            Message message(PIPE_COMMAND_TITLE_CHANGED);
            message.SetValue(PIPE_PROPERTY_VALUE, oldTitle);
            ::SendToOwner(message);
        }
    }

    wchar_t* env = ::GetEnvironmentStrings();
    if (env)
    {
        size_t len = 0;
        for (const wchar_t* cur = env; cur[0] || (cur > env && cur[-1]); cur++, len++)
        {
            // find the second null in a row
        }

        if (len != oldEnvironment.size() || ::memcmp(env, oldEnvironment.c_str(), len))
        {
            oldEnvironment.assign(env, len);

            Message message(PIPE_COMMAND_ENV_CHANGED);
            message.ParseNameValuePairs(env, '\0');
            ::SendToOwner(message);
        }

        ::FreeEnvironmentStrings(env);
    }
}

void DevInject::CheckConsoleWindowSize(bool visibleOnly)
{
    HWND hwnd = ::GetConsoleWindow();
    if (hwnd && (!visibleOnly || ::IsWindowVisible(hwnd)))
    {
        HWND parent = ::GetParent(hwnd);
        if (parent)
        {
            RECT rect, rect2;
            if (::GetWindowRect(parent, &rect) && ::GetWindowRect(hwnd, &rect2) && ::memcmp(&rect, &rect2, sizeof(rect)))
            {
                ::SetWindowPos(hwnd, nullptr, 0, 0, rect.right - rect.left, rect.bottom - rect.top,
                    SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE | SWP_NOCOPYBITS);
            }
        }
    }
}

// A thread that detects if the owner process has died, and if it does then kills this process too.
// Normally the owner process will politely close all console processes first, but it might crash.
static DWORD __stdcall WatchdogThread(void*)
{
    std::array<HANDLE, 2> handles = { ::ownerProcess, ::disposeEvent };
    std::wstring oldTitle;
    std::wstring oldEnvironment;

    while (true)
    {
        switch (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, 2048))
        {
        case WAIT_OBJECT_0:
            ::TerminateProcess(::GetCurrentProcess(), 0);
            break;

        case WAIT_TIMEOUT:
            ::NotifyOwnerOfChanges(oldTitle, oldEnvironment);
            DevInject::CheckConsoleWindowSize(true);
            break;

        default:
            return 0;
        }
    }
}

// A thread that only runs until it can find that a main console window has been created
static DWORD __stdcall FindMainWindowThread(void*)
{
    HWND hwnd = ::GetConsoleWindow();

    std::array<HANDLE, 2> handles = { ::disposeEvent, ::ownerProcess };
    while (!hwnd && ::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, 50) == WAIT_TIMEOUT)
    {
        hwnd = ::GetConsoleWindow();
    }

    if (hwnd)
    {
        Message message(PIPE_COMMAND_WINDOW_CREATED);
        message.SetValue(PIPE_PROPERTY_VALUE, std::to_wstring(reinterpret_cast<size_t>(hwnd)));
        ::SendToOwner(message);
    }

    return 0;
}

static BOOL CALLBACK FindOwnerProcessWindow(HWND hwnd, LPARAM lp)
{
    DWORD* processId = reinterpret_cast<DWORD*>(lp);
    if (!*processId)
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
                        ::ownerPipe = Pipe::Connect(hwndProcess, ::disposeEvent);
                        if (::ownerPipe)
                        {
                            *processId = hwndProcessId;
                        }
                    }
                }

                ::CloseHandle(hwndProcess);
            }
        }
    }

    return TRUE;
}

static HANDLE OpenOwnerProcess()
{
    assert(::disposeEvent && !::ownerProcess && !::ownerPipe);

    DWORD processId = 0;
    ::EnumDesktopWindows(nullptr, ::FindOwnerProcessWindow, reinterpret_cast<LPARAM>(&processId));

    return (processId && processId != ::GetCurrentProcessId())
        ? ::OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE, FALSE, processId)
        : nullptr;
}

// Called when the DLL is mapped into any process
void DevInject::Initialize(HMODULE module)
{
    ::module = module;
    ::disposeEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);
    ::ownerProcess = ::OpenOwnerProcess();

    // No owner process unless this DLL was injected
    if (::ownerProcess)
    {
        assert(::ownerPipe);

        // Don't use std::thread since it will wait for the thread to start running
        // for some reason, and that will hang since that thread can't get the loader lock.
        ::pipeServerThread = ::CreateThread(nullptr, 0, ::PipeServerThread, nullptr, 0, nullptr);
        ::watchdogThread = ::CreateThread(nullptr, 0, ::WatchdogThread, nullptr, 0, nullptr);
        ::findMainWindowThread = ::CreateThread(nullptr, 0, ::FindMainWindowThread, nullptr, 0, nullptr);
    }
    else
    {
        assert(!::ownerPipe);

        ::CloseHandle(::disposeEvent);
        ::disposeEvent = nullptr;
    }
}

// Called from a command handler
HMODULE DevInject::Dispose()
{
    if (::ownerProcess)
    {
        ::SetEvent(::disposeEvent);

        ::WaitForSingleObject(::pipeServerThread, INFINITE);
        ::WaitForSingleObject(::watchdogThread, INFINITE);
        ::WaitForSingleObject(::findMainWindowThread, INFINITE);

        ::ownerPipeMutex.lock();
        ::ownerPipe.Dispose();
        ::ownerPipeMutex.unlock();

        ::CloseHandle(::pipeServerThread);
        ::CloseHandle(::watchdogThread);
        ::CloseHandle(::findMainWindowThread);
        ::CloseHandle(::disposeEvent);
        ::CloseHandle(::ownerProcess);

        ::pipeServerThread = nullptr;
        ::watchdogThread = nullptr;
        ::findMainWindowThread = nullptr;
        ::disposeEvent = nullptr;
        ::ownerProcess = nullptr;
    }

    return ::module;
}

HMODULE DevInject::GetModule()
{
    return ::module;
}
