#include "stdafx.h"
#include "App.h"
#include "DevPrompt_h.h"
#include "Json/Persist.h"
#include "Process2.h"
#include "Utility.h"

Process::Process(App& app)
    : app(app.shared_from_this())
    , disposeEvent(::CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS))
    , messageEvent(::CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS))
    , hostWnd(nullptr)
    , processId(0)
{
    this->app->OnProcessCreated(this);
}

Process::~Process()
{
    this->Dispose();

    if (this->backgroundThread.joinable())
    {
        if (this->backgroundThread.get_id() != std::this_thread::get_id())
        {
            this->backgroundThread.join();
        }
        else
        {
            this->backgroundThread.detach();
        }
    }

    if (this->injectConhostThread.joinable())
    {
        if (this->injectConhostThread.get_id() != std::this_thread::get_id())
        {
            this->injectConhostThread.join();
        }
        else
        {
            this->injectConhostThread.detach();
        }
    }

    ::CloseHandle(this->messageEvent);
    ::CloseHandle(this->disposeEvent);

    this->app->OnProcessDestroyed(this);
}

void Process::Initialize(HWND processHostWindow)
{
    assert(App::IsMainThread() && processHostWindow);

    WNDCLASSEX windowClass{};
    windowClass.cbSize = sizeof(windowClass);
    windowClass.hInstance = this->app->GetInstance();
    windowClass.lpszClassName = L"Process::Host";
    windowClass.hCursor = ::LoadCursor(nullptr, IDC_ARROW);
    windowClass.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_BTNFACE + 1);

    RECT rect;
    ::GetClientRect(processHostWindow, &rect);
    this->hostWnd = IWindowProc::Create(this, windowClass, WS_CHILD | WS_CLIPCHILDREN, rect, processHostWindow);
}

void Process::Dispose()
{
    if (App::IsMainThread() && this->hostWnd)
    {
        ::DestroyWindow(this->hostWnd);
    }

    ::SetEvent(this->disposeEvent);
}

void Process::Detach()
{
    assert(App::IsMainThread());

    if (this->GetProcessId())
    {
        this->SetChildWindow(nullptr);
        ::InterlockedExchange(&this->processId, 0);
        this->SendMessageAsync(PIPE_COMMAND_DETACH);
    }

    this->Dispose();
}

bool Process::Attach(HANDLE process)
{
    assert(App::IsMainThread());

    HANDLE dupeProcess = nullptr;
    if (::DuplicateHandle(::GetCurrentProcess(), process, ::GetCurrentProcess(), &dupeProcess, PROCESS_ALL_ACCESS, TRUE, 0))
    {
        std::shared_ptr<Process> self = shared_from_this();

        this->backgroundThread = std::thread([self, dupeProcess]()
        {
            self->BackgroundAttach(dupeProcess);
        });

        return true;
    }

    return false;
}

bool Process::Start(const Json::Dict& info)
{
    assert(App::IsMainThread());

    std::shared_ptr<Process> self = shared_from_this();

    this->backgroundThread = std::thread([self, info]()
    {
        self->BackgroundStart(info);
    });

    return true;
}

bool Process::Clone(const std::shared_ptr<Process>& process)
{
    assert(App::IsMainThread());

    std::shared_ptr<Process> self = shared_from_this();

    this->backgroundThread = std::thread([self, process]()
    {
        self->BackgroundClone(process);
    });

    return true;
}

HWND Process::GetHostWindow() const
{
    assert(App::IsMainThread());

    return this->hostWnd;
}

DWORD Process::GetProcessId() const
{
    return ::InterlockedXor(const_cast<long*>(reinterpret_cast<const long*>(&this->processId)), 0);
}

std::wstring Process::GetProcessState()
{
    assert(App::IsMainThread());

    Json::Dict output;
    if (this->TransactMessage(PIPE_COMMAND_GET_STATE, output))
    {
        return Json::Write(output);
    }

    return std::wstring();
}

void Process::SendDpiChanged()
{
    assert(App::IsMainThread());

    this->SendMessageAsync(PIPE_COMMAND_CHECK_WINDOW_DPI);
}

void Process::SendSystemCommand(UINT id)
{
    assert(App::IsMainThread());

    HWND hwnd = this->GetChildWindow();
    if (hwnd)
    {
        if (id == SC_CLOSE)
        {
            ::SendMessage(hwnd, WM_SYSCOMMAND, id, 0);
        }
        else
        {
            ::PostMessage(hwnd, WM_SYSCOMMAND, id, 0);
        }
    }
}

void Process::Activate()
{
    assert(App::IsMainThread());

    if (this->hostWnd)
    {
        ::ShowWindow(this->hostWnd, SW_SHOW);
    }

    this->SendMessageAsync(PIPE_COMMAND_ACTIVATED);
}

void Process::Deactivate()
{
    assert(App::IsMainThread());

    if (this->hostWnd)
    {
        ::ShowWindow(this->hostWnd, SW_HIDE);
    }

    this->SendMessageAsync(PIPE_COMMAND_DEACTIVATED);
}

bool Process::IsActive()
{
    assert(App::IsMainThread());

    if (this->hostWnd)
    {
        int style = ::GetWindowLong(this->hostWnd, GWL_STYLE);
        return (style & WS_VISIBLE) == WS_VISIBLE;
    }

    return false;
}

void Process::SetChildWindow(HWND hwnd)
{
    assert(App::IsMainThread());

    if (this->hostWnd)
    {
        HWND childHwnd = this->GetChildWindow();

        if (hwnd && !childHwnd)
        {
            UINT oldDpi = ::GetDpiForWindow(hwnd);
            LONG style = ::GetWindowLong(hwnd, GWL_STYLE);
            style = style & ~(WS_CAPTION | WS_BORDER | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_CLIPSIBLINGS) | WS_CHILD;

            LONG exstyle = ::GetWindowLong(hwnd, GWL_EXSTYLE);
            exstyle = exstyle & ~(WS_EX_APPWINDOW | WS_EX_LAYERED);

            ::SetWindowLong(hwnd, GWL_STYLE, style);
            ::SetWindowLong(hwnd, GWL_EXSTYLE, exstyle);
            ::SetParent(hwnd, this->hostWnd);

            if (::GetDpiForWindow(hwnd) != oldDpi)
            {
                // Update DPI before the window is visible
                this->TransactMessage(PIPE_COMMAND_CHECK_WINDOW_DPI);
            }

            RECT rect;
            ::GetClientRect(this->hostWnd, &rect);
            ::SetWindowPos(hwnd, HWND_TOP, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            if (::GetFocus() == this->hostWnd)
            {
                ::SetFocus(hwnd);
            }

            // Now's the chance to ask the process for a bunch of info
            this->SendMessageAsync(PIPE_COMMAND_GET_STATE);
            this->SendMessageAsync(PIPE_COMMAND_CHECK_WINDOW_SIZE);
            this->InjectConhost(hwnd);
        }
        else if (!hwnd && childHwnd)
        {
            this->app->NoAutoGrabWindow(childHwnd);

            RECT rect;
            ::GetWindowRect(this->hostWnd, &rect);
            ::ShowWindow(childHwnd, SW_HIDE);

            LONG style = WS_OVERLAPPEDWINDOW | WS_CLIPSIBLINGS | WS_VSCROLL;
            LONG exstyle = ::GetWindowLong(childHwnd, GWL_EXSTYLE) | WS_EX_APPWINDOW; // | WS_EX_LAYERED;

            ::SetParent(childHwnd, nullptr);
            ::SetWindowLong(childHwnd, GWL_STYLE, style);
            ::SetWindowLong(childHwnd, GWL_EXSTYLE, exstyle);

            ::SetWindowPos(childHwnd, HWND_TOP, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            ::SetForegroundWindow(childHwnd);
            ::PostMessage(childHwnd, DevInject::GetDetachMessage(), 0, 0);
        }
    }
}

LRESULT Process::WindowProc(HWND hwnd, UINT message, WPARAM wp, LPARAM lp)
{
    assert(App::IsMainThread());

    switch (message)
    {
    case WM_SIZE:
        this->SendMessageAsync(PIPE_COMMAND_CHECK_WINDOW_SIZE);
        break;

    case WM_SETFOCUS:
        WindowProc::FocusFirstChild(hwnd);
        break;

    case WM_DESTROY:
        this->app->OnProcessClosing(this);
        this->SendSystemCommand(SC_CLOSE);
        this->SendMessageAsync(PIPE_COMMAND_CLOSED);
        this->hostWnd = nullptr;
        break;

    case WM_PAINT:
        WindowProc::PaintMessage(hwnd, this->app->GetMessageFont(hwnd), L"Waiting for process to respond...");
        break;

    case WM_CHAR:
    case WM_KEYDOWN:
    case WM_KEYUP:
    case WM_SYSCHAR:
    case WM_SYSKEYDOWN:
    case WM_SYSKEYUP:
        ::PostMessage(::GetAncestor(hwnd, GA_ROOT), message, wp, lp);
        break;
    }

    return ::DefWindowProc(hwnd, message, wp, lp);
}

HWND Process::GetChildWindow() const
{
    assert(App::IsMainThread());

    return this->hostWnd ? ::GetTopWindow(this->hostWnd) : nullptr;
}

void Process::PostDispose()
{
    std::shared_ptr<Process> self = shared_from_this();

    this->app->PostToMainThread([self]()
    {
        self->Dispose();
    });
}

void Process::InjectConhost(HWND conhostHwnd)
{
    assert(App::IsMainThread());

    std::shared_ptr<Process> self = shared_from_this();

    this->injectConhostThread = std::thread([self, conhostHwnd]()
    {
        self->BackgroundInjectConhost(conhostHwnd);
    });
}

// Creates a pipe server to listen to the other process, and injects a thread
// into that process to create another pipe server that listens to this process. Whew...
void Process::BackgroundAttach(HANDLE process, HANDLE mainThread, const Json::Dict* info)
{
    assert(!App::IsMainThread());
    std::shared_ptr<Process> self = shared_from_this();
    ::InterlockedExchange(&this->processId, ::GetProcessId(process));

    Pipe pipe = Pipe::Create(process, this->disposeEvent);
    bool injected = DevInject::InjectDll(process, this->disposeEvent, true);

    if (injected)
    {
        if (mainThread)
        {
            ::ResumeThread(mainThread);
        }

        if (info)
        {
            this->InitNewProcess(*info);
        }

        if (pipe.WaitForClient())
        {
            std::thread sendCommandsThread([self, process]()
            {
                self->BackgroundSendCommands(process);
            });

            pipe.RunServer([self, process](const Json::Dict& input)
            {
                return self->HandleMessage(process, input);
            });

            sendCommandsThread.join();

            this->FlushRemainingMessages(process);
        }
    }

    this->PostDispose();

    // Make sure we didn't detach from the process before waiting for it or killing it
    if (GetProcessId())
    {
        if (!injected && mainThread)
        {
            // We created the process, so we have to kill it when injection fails
            ::TerminateProcess(process, 0);
        }

        if (injected || mainThread)
        {
            // The process will die if we created it or if injection succeeded
            ::WaitForSingleObject(process, INFINITE);
        }
    }

    if (mainThread)
    {
        ::CloseHandle(mainThread);
    }

    ::InterlockedExchange(&this->processId, 0);
    ::CloseHandle(process);
}

static bool DirectoryExists(const wchar_t* path)
{
    WIN32_FIND_DATA fd;
    HANDLE handle = ::FindFirstFileEx(path, FindExInfoStandard, &fd, FindExSearchLimitToDirectories, nullptr, 0);
    if (handle != INVALID_HANDLE_VALUE)
    {
        ::FindClose(handle);
        return (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
    }

    // Check just a drive letter
    wchar_t letter = ::towlower(path[0]);
    if (letter >= 'a' && letter <= 'z' && path[1] == ':' && ((path[2] == '\\' && path[3] == '\0') || path[2] == '\0'))
    {
        DWORD drives = ::GetLogicalDrives();
        DWORD checkDrive = 1 << (letter - 'a');
        if ((drives & checkDrive) != 0)
        {
            return true;
        }
    }

    return false;
}

void Process::BackgroundStart(const Json::Dict& info)
{
    assert(!App::IsMainThread());

    std::wstring environment;
    std::wstring executable = info.Get(PIPE_PROPERTY_EXECUTABLE).TryGetString();
    std::wstring arguments = info.Get(PIPE_PROPERTY_ARGUMENTS).TryGetString();
    std::wstring directory = info.Get(PIPE_PROPERTY_DIRECTORY).TryGetString();

    if (info.Get(PIPE_PROPERTY_ENVIRONMENT).IsDict())
    {
        environment = Json::WriteNameValuePairs(info.Get(PIPE_PROPERTY_ENVIRONMENT).GetDict(), L'\0');
    }

    std::wstringstream commandLine;
    commandLine << L"\"" << executable << L"\" " << arguments;
    std::wstring commandLineString = commandLine.str();
    wchar_t* commandLineBuffer = const_cast<wchar_t*>(commandLineString.c_str());
    void* envBlock = const_cast<wchar_t*>(environment.size() > 1 ? environment.c_str() : nullptr);

    const wchar_t* startingDirectory = directory.size() ? directory.c_str() : nullptr;
    if (startingDirectory && !::DirectoryExists(startingDirectory))
    {
        startingDirectory = nullptr;
    }

    STARTUPINFO si{};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;

    PROCESS_INFORMATION pi;
    DWORD flags = CREATE_NEW_CONSOLE | CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT;

    SECURITY_ATTRIBUTES processSecurity;
    processSecurity.nLength = sizeof(processSecurity);
    processSecurity.lpSecurityDescriptor = nullptr;
    processSecurity.bInheritHandle = TRUE;

    if (::CreateProcess(nullptr, commandLineBuffer, &processSecurity, nullptr, FALSE, flags, envBlock, startingDirectory, &si, &pi))
    {
        this->BackgroundAttach(pi.hProcess, pi.hThread, &info);
    }
    else
    {
        // Need a better way to notify that the process wasn't created rather than just delete the tab
        this->PostDispose();
    }
}

void Process::BackgroundClone(const std::shared_ptr<Process>& process)
{
    assert(!App::IsMainThread());

    Json::Dict output;
    if (process->TransactMessage(PIPE_COMMAND_GET_STATE, output))
    {
        this->BackgroundStart(output);
    }
    else
    {
        this->PostDispose();
    }
}

// Thread that just sends messages to the other pipe server when a command is put in a queue
void Process::BackgroundSendCommands(HANDLE process)
{
    std::array<HANDLE, 3> handles = { this->messageEvent, this->disposeEvent, process };
    while (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
    {
        std::vector<Json::Dict> messages;
        {
            std::scoped_lock<std::mutex> pipeLock(this->processPipeMutex);
            std::scoped_lock<std::mutex> lock(this->messageMutex);

            if (this->processPipe)
            {
                messages = std::move(this->messages);
            }

            ::ResetEvent(this->messageEvent);
        }

        this->SendMessages(process, messages);
    }
}

void Process::BackgroundInjectConhost(HWND conhostHwnd)
{
    std::shared_ptr<Process> self = shared_from_this();
    DWORD conhostProcessId = 0;
    HANDLE conhostProcess = nullptr;

    HANDLE snap = ::CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap && snap != INVALID_HANDLE_VALUE)
    {
        ::PROCESSENTRY32 entry;
        entry.dwSize = sizeof(entry);

        for (BOOL status = ::Process32First(snap, &entry); status; status = ::Process32Next(snap, &entry))
        {
            if (entry.th32ParentProcessID == this->GetProcessId())
            {
                conhostProcessId = entry.th32ProcessID;
                conhostProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, conhostProcessId);
                break;
            }
        }

        ::CloseHandle(snap);
    }

    assert(conhostProcess);
    if (conhostProcess)
    {
        Pipe pipe = Pipe::Create(conhostProcess, this->disposeEvent);
        bool injected = DevInject::InjectDll(conhostProcess, this->disposeEvent, true);
        if (injected)
        {
            if (pipe.WaitForClient())
            {
                pipe.RunServer([self, conhostProcess, conhostHwnd](const Json::Dict& input)
                {
                    return self->HandleConhostMessage(conhostProcess, conhostHwnd, input);
                });
            }
        }

        ::CloseHandle(conhostProcess);
    }
}

// Initialize a newly created process after pipes are connected
void Process::InitNewProcess(const Json::Dict& info)
{
    Json::Dict infoCopy = info;

    // These values are only useful when first creating the process
    infoCopy.Set(PIPE_PROPERTY_ARGUMENTS, Json::Value());
    infoCopy.Set(PIPE_PROPERTY_ENVIRONMENT, Json::Value());
    infoCopy.Set(PIPE_PROPERTY_EXECUTABLE, Json::Value());
    infoCopy.Set(PIPE_PROPERTY_DIRECTORY, Json::Value());

    if (infoCopy.Size())
    {
        infoCopy.Set(PIPE_PROPERTY_COMMAND, Json::Value(PIPE_COMMAND_SET_STATE));
        this->SendMessageAsync(std::move(infoCopy));
    }
}

void Process::SendMessageAsync(std::wstring&& name)
{
    Json::Dict message;
    message.Set(PIPE_PROPERTY_COMMAND, Json::Value(std::move(name)));
    this->SendMessageAsync(std::move(message));
}

// Adds a command to the queue to send to the other process. It may never be sent if the other process dies.
void Process::SendMessageAsync(Json::Dict&& command)
{
    // call from any thread

    std::scoped_lock<std::mutex> lock(this->messageMutex);
    this->messages.push_back(std::move(command));
    ::SetEvent(this->messageEvent);
}

void Process::SendMessages(HANDLE process, const std::vector<Json::Dict>& messages)
{
    for (const Json::Dict& message : messages)
    {
        Json::Dict output;
        bool status = false;
        {
            std::scoped_lock<std::mutex> lock(this->processPipeMutex);
            status = this->processPipe && this->processPipe.Transact(message, output);
        }

        if (status)
        {
            Json::Value name = message.Get(PIPE_PROPERTY_COMMAND);
            this->HandleResponse(name.TryGetString(), output);
        }
    }
}

// Blocks while a command is sent
bool Process::TransactMessage(std::wstring&& name)
{
    Json::Dict output;
    return this->TransactMessage(std::move(name), output);
}

// Blocks while a command is sent
bool Process::TransactMessage(std::wstring&& name, Json::Dict& output)
{
    Json::Dict message;
    message.Set(PIPE_PROPERTY_COMMAND, Json::Value(std::move(name)));
    return this->TransactMessage(message, output);
}

// Blocks while a command is sent
bool Process::TransactMessage(const Json::Dict& input, Json::Dict& output)
{
    Json::Value name = input.Get(PIPE_PROPERTY_COMMAND);
    bool result = false;
    {
        std::scoped_lock<std::mutex> lock(this->processPipeMutex);
        result = this->processPipe && this->processPipe.Transact(input, output);
    }

    if (result)
    {
        this->HandleResponse(name.TryGetString(), output);
    }

    return result;
}

// Called after the BackgroundSendCommands thread is gone to flush any remaining messages
void Process::FlushRemainingMessages(HANDLE process)
{
    while (true)
    {
        std::vector<Json::Dict> messages;
        {
            std::scoped_lock<std::mutex> lock(this->messageMutex);
            messages = std::move(this->messages);
            ::ResetEvent(this->messageEvent);
        }

        if (messages.size())
        {
            this->SendMessages(process, messages);
        }
        else
        {
            break;
        }
    }
}

// Handles messages that come in from the other process through my pipe server
Json::Dict Process::HandleMessage(HANDLE process, const Json::Dict& input)
{
    assert(!App::IsMainThread());

    Json::Dict result;
    std::shared_ptr<Process> self = this->shared_from_this();
    std::wstring name = input.Get(PIPE_PROPERTY_COMMAND).TryGetString();

    if (name == PIPE_COMMAND_PIPE_CREATED)
    {
        Pipe info = Pipe::Connect(process, this->disposeEvent);

        std::scoped_lock<std::mutex> pipeLock(this->processPipeMutex);
        this->processPipe = std::move(info);
        {
            std::scoped_lock<std::mutex> commandLock(this->messageMutex);
            ::SetEvent(this->messageEvent);
        }
    }
    else if (name == PIPE_COMMAND_WINDOW_CREATED)
    {
        HWND hwnd = input.Get(PIPE_PROPERTY_HWND).TryGetHwndFromString();
        if (hwnd)
        {
            this->app->PostToMainThread([self, hwnd]()
            {
                self->SetChildWindow(hwnd);
            }, true);
        }
    }
    else if (name == PIPE_COMMAND_STATE_CHANGED)
    {
        this->HandleNewState(input);
    }

    return result;
}

Json::Dict Process::HandleConhostMessage(HANDLE process, HWND conhostHwnd, const Json::Dict& input)
{
    assert(!App::IsMainThread());

    Json::Dict result;
    std::wstring name = input.Get(PIPE_PROPERTY_COMMAND).TryGetString();

    if (name == PIPE_COMMAND_CONHOST_INJECTED)
    {
        result.Set(PIPE_PROPERTY_HWND, Json::Value(std::to_wstring(reinterpret_cast<size_t>(conhostHwnd))));
    }

    return result;
}

// Handles command responses that come from the other process after we send it a command
void Process::HandleResponse(const std::wstring& name, const Json::Dict& output)
{
    if (name == PIPE_COMMAND_GET_STATE)
    {
        this->HandleNewState(output);
    }
}

// Handles PIPE_COMMAND_GET_STATE response or PIPE_COMMAND_STATE_CHANGED message
void Process::HandleNewState(const Json::Dict& state)
{
    std::shared_ptr<Process> self = this->shared_from_this();

    Json::Value title = state.Get(PIPE_PROPERTY_TITLE);
    if (title.IsString())
    {
        this->app->PostToMainThread([self, title]()
        {
            self->app->OnProcessTitleChanged(self.get(), title.GetString());
        }, true);
    }

    Json::Value environment = state.Get(PIPE_PROPERTY_ENVIRONMENT);
    if (environment.IsDict())
    {
        this->app->PostToMainThread([self, environment]()
        {
            self->app->OnProcessEnvChanged(self.get(), environment.GetDict());
        }, true);
    }
}
