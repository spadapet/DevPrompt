#include "stdafx.h"
#include "Context/AppContext.h"
#include "Context/AppMessageHandler.h"
#include "Json/Persist.h"
#include "Pipe.h"
#include "Utility.h"

static HANDLE disposeEvent = nullptr;
static HANDLE ownerProcess = nullptr;
static HANDLE pipeServerThread = nullptr;
static HANDLE watchdogThread = nullptr;
static HANDLE findMainWindowThread = nullptr;
static std::mutex ownerPipeMutex;
static Pipe ownerPipe;

static void SendToOwner(const Json::Dict& message)
{
    std::scoped_lock<std::mutex> lock(::ownerPipeMutex);
    if (::ownerPipe)
    {
        ::ownerPipe.Send(message);
    }
}

// A thread that listens for commands coming from the owner process, and responds to them
static DWORD __stdcall PipeServerThread(void*)
{
    Pipe pipe = Pipe::Create(::ownerProcess, ::disposeEvent);

    ::SendToOwner(Json::CreateMessage(PIPE_COMMAND_PIPE_CREATED));

    if (pipe.WaitForClient())
    {
        pipe.RunServer(DevInject::CreateMessageHandler());
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

            Json::Dict message = Json::CreateMessage(PIPE_COMMAND_STATE_CHANGED);
            message.Set(PIPE_PROPERTY_TITLE, Json::Value(std::wstring(oldTitle)));
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

            Json::Dict message = Json::CreateMessage(PIPE_COMMAND_STATE_CHANGED);
            message.Set(PIPE_PROPERTY_ENVIRONMENT, Json::Value(Json::ParseNameValuePairs(env, '\0')));
            ::SendToOwner(message);
        }

        ::FreeEnvironmentStrings(env);
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
        Json::Dict message = Json::CreateMessage(PIPE_COMMAND_WINDOW_CREATED);
        message.Set(PIPE_PROPERTY_HWND, Json::Value(std::to_wstring(reinterpret_cast<size_t>(hwnd))));
        ::SendToOwner(message);
    }

    return 0;
}

AppContext::AppContext()
{
}

AppContext::~AppContext()
{
}

void AppContext::Initialize()
{
    ::disposeEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);
    ::ownerProcess = this->OpenOwnerProcess(::disposeEvent, ::ownerPipe);

    if (::ownerProcess)
    {
        assert(::ownerPipe);

        // Don't use std::thread since it will wait for the thread to start running
        // for some reason, and that will hang since that thread can't get the loader lock.
        ::watchdogThread = ::CreateThread(nullptr, 0, ::WatchdogThread, nullptr, 0, nullptr);
        ::pipeServerThread = ::CreateThread(nullptr, 0, ::PipeServerThread, nullptr, 0, nullptr);
        ::findMainWindowThread = ::CreateThread(nullptr, 0, ::FindMainWindowThread, nullptr, 0, nullptr);
    }
}

void AppContext::Dispose()
{
    ::SetEvent(::disposeEvent);

    if (::findMainWindowThread)
    {
        ::WaitForSingleObject(::findMainWindowThread, INFINITE);
        ::CloseHandle(::findMainWindowThread);
        ::findMainWindowThread = nullptr;
    }

    if (::pipeServerThread)
    {
        ::WaitForSingleObject(::pipeServerThread, INFINITE);
        ::CloseHandle(::pipeServerThread);
        ::pipeServerThread = nullptr;
    }

    if (::watchdogThread)
    {
        ::WaitForSingleObject(::watchdogThread, INFINITE);
        ::CloseHandle(::watchdogThread);
        ::watchdogThread = nullptr;
    }

    if (::ownerPipe)
    {
        std::scoped_lock<std::mutex> lock(::ownerPipeMutex);
        ::ownerPipe.Dispose();
    }

    if (::ownerProcess)
    {
        ::CloseHandle(::ownerProcess);
        ::ownerProcess = nullptr;
    }

    ::CloseHandle(::disposeEvent);
    ::disposeEvent = nullptr;
}
