#include "stdafx.h"
#include "App.h"
#include "Process2.h"
#include "../DevInject/Inject.h"

Process::Process(App& app)
    : app(app.shared_from_this())
    , disposeEvent(::CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS))
    , commandEvent(::CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS))
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

    ::CloseHandle(this->commandEvent);
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

    if (GetProcessId())
    {
        this->SetChildWindow(nullptr);
        ::InterlockedExchange(&this->processId, 0);
        this->SendCommandAsync(Message(PIPE_COMMAND_DETACH));
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

bool Process::Start(const ProcessStartInfo& info)
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

std::wstring Process::GetProcessExe()
{
    assert(App::IsMainThread());

    return this->processExe;
}

std::wstring Process::GetProcessWindowTitle()
{
    assert(App::IsMainThread());

    return this->processWindowTitle;
}

std::wstring Process::GetProcessEnv()
{
    assert(App::IsMainThread());

    return this->processEnv;
}

std::wstring Process::GetProcessAliases()
{
    assert(App::IsMainThread());

    Message response;
    if (this->TransactCommand(Message(PIPE_COMMAND_GET_ALIASES), response))
    {
        return response.GetNamesAndValues();
    }

    return std::wstring();
}

std::wstring Process::GetProcessCurrentDirectory()
{
    assert(App::IsMainThread());

    Message response;
    if (this->TransactCommand(Message(PIPE_COMMAND_GET_CURRENT_DIRECTORY), response))
    {
        return response.GetValue(PIPE_PROPERTY_VALUE);
    }

    return std::wstring();
}

std::wstring Process::GetProcessColorTable()
{
    assert(App::IsMainThread());

    Message response;
    if (this->TransactCommand(Message(PIPE_COMMAND_GET_COLOR_TABLE), response))
    {
        return response.GetValue(PIPE_PROPERTY_VALUE);
    }

    return std::wstring();
}

void Process::SetProcessColorTable(const wchar_t* value)
{
    assert(App::IsMainThread());

    Message command(PIPE_COMMAND_SET_COLOR_TABLE);
    command.SetValue(PIPE_PROPERTY_VALUE, value);

    this->SendCommandAsync(std::move(command));
}

void Process::SendDpiChanged(double oldScale, double newScale)
{
    assert(App::IsMainThread());

    this->SendCommandAsync(Message(PIPE_COMMAND_CHECK_WINDOW_DPI));
}

void Process::SendSystemCommand(UINT id)
{
    assert(App::IsMainThread());

    if (this->GetChildWindow())
    {
        if (id == SC_CLOSE)
        {
            ::SendMessage(this->GetChildWindow(), WM_SYSCOMMAND, id, 0);
        }
        else
        {
            ::PostMessage(this->GetChildWindow(), WM_SYSCOMMAND, id, 0);
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

    this->SendCommandAsync(Message(PIPE_COMMAND_ACTIVATED));
}

void Process::Deactivate()
{
    assert(App::IsMainThread());

    if (this->hostWnd)
    {
        ::ShowWindow(this->hostWnd, SW_HIDE);
    }

    this->SendCommandAsync(Message(PIPE_COMMAND_DEACTIVATED));
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
        if (hwnd && !this->GetChildWindow())
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
                this->TransactCommand(Message(PIPE_COMMAND_CHECK_WINDOW_DPI));
            }

            RECT rect;
            ::GetClientRect(this->hostWnd, &rect);
            ::SetWindowPos(hwnd, HWND_TOP, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            if (::GetFocus() == this->hostWnd)
            {
                ::SetFocus(hwnd);
            }

            // Now's the chance to ask the process for a bunch of info
            this->SendCommandAsync(Message(PIPE_COMMAND_GET_EXE));
            this->SendCommandAsync(Message(PIPE_COMMAND_GET_TITLE));
            this->SendCommandAsync(Message(PIPE_COMMAND_GET_ENV));
            this->SendCommandAsync(Message(PIPE_COMMAND_CHECK_WINDOW_SIZE));
        }
        else if (!hwnd && this->GetChildWindow())
        {
            hwnd = this->GetChildWindow();
            this->app->NoAutoGrabWindow(hwnd);

            RECT rect;
            ::GetWindowRect(this->hostWnd, &rect);
            ::ShowWindow(hwnd, SW_HIDE);

            LONG style = WS_OVERLAPPEDWINDOW | WS_CLIPSIBLINGS | WS_VSCROLL;
            LONG exstyle = ::GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_APPWINDOW; // | WS_EX_LAYERED;

            ::SetParent(hwnd, nullptr);
            ::SetWindowLong(hwnd, GWL_STYLE, style);
            ::SetWindowLong(hwnd, GWL_EXSTYLE, exstyle);

            ::SetWindowPos(hwnd, HWND_TOP, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            ::SetForegroundWindow(hwnd);
        }
    }
}

LRESULT Process::WindowProc(HWND hwnd, UINT message, WPARAM wp, LPARAM lp)
{
    assert(App::IsMainThread());

    switch (message)
    {
    case WM_SIZE:
        this->SendCommandAsync(Message(PIPE_COMMAND_CHECK_WINDOW_SIZE));
        break;

    case WM_SETFOCUS:
        WindowProc::FocusFirstChild(hwnd);
        break;

    case WM_DESTROY:
        this->app->OnProcessClosing(this);
        this->SendCommandAsync(Message(PIPE_COMMAND_CLOSED));

        if (this->GetChildWindow())
        {
            this->SendSystemCommand(SC_CLOSE);
        }

        this->hostWnd = nullptr;
        break;

    case WM_PAINT:
        WindowProc::PaintMessage(hwnd, this->app->GetMessageFont(hwnd), L"Waiting for process to respond...");
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

// Creates a pipe server to listen to the other process, and injects a thread
// into that process to create another pipe server that listens to this process. Whew...
void Process::BackgroundAttach(HANDLE process, HANDLE mainThread, const ProcessStartInfo* info)
{
    assert(!App::IsMainThread());
    std::shared_ptr<Process> self = shared_from_this();
    ::InterlockedExchange(&this->processId, ::GetProcessId(process));

    Pipe pipe = Pipe::Create(process, self->disposeEvent);
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

            pipe.RunServer([self, process](const Message & input)
                {
                    return self->CommandHandler(process, input);
                });

            sendCommandsThread.join();

            this->FlushRemainingCommands();
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

void Process::BackgroundStart(const ProcessStartInfo& info)
{
    assert(!App::IsMainThread());

    std::wstring environment = info.environment;
    std::replace(environment.begin(), environment.end(), L'\n', L'\0');
    environment += L'\0'; // just in case

    std::wstringstream commandLine;
    commandLine << L"\"" << info.executable << L"\" " << info.arguments;
    std::wstring commandLineString = commandLine.str();
    wchar_t* commandLineBuffer = const_cast<wchar_t*>(commandLineString.c_str());
    void* envBlock = const_cast<wchar_t*>(environment.size() > 1 ? environment.c_str() : nullptr);

    const wchar_t* startingDirectory = info.startingDirectory.size() ? info.startingDirectory.c_str() : nullptr;
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
    std::shared_ptr<Process> self = shared_from_this();

    Message exeResponse;
    Message dirResponse;
    Message envResponse;
    Message aliasesResponse;
    Message colorTableResponse;
    Message titleResponse;

    if (!process->TransactCommand(Message(PIPE_COMMAND_GET_EXE), exeResponse) || exeResponse.GetValue(PIPE_PROPERTY_VALUE).empty() ||
        !process->TransactCommand(Message(PIPE_COMMAND_GET_CURRENT_DIRECTORY), dirResponse) || dirResponse.GetValue(PIPE_PROPERTY_VALUE).empty() ||
        !process->TransactCommand(Message(PIPE_COMMAND_GET_ENV), envResponse) ||
        !process->TransactCommand(Message(PIPE_COMMAND_GET_ALIASES), aliasesResponse) ||
        !process->TransactCommand(Message(PIPE_COMMAND_GET_COLOR_TABLE), colorTableResponse) ||
        !process->TransactCommand(Message(PIPE_COMMAND_GET_TITLE), titleResponse))
    {
        this->PostDispose();
        return;
    }

    ProcessStartInfo info;
    info.executable = exeResponse.GetValue(PIPE_PROPERTY_VALUE);
    info.environment = envResponse.GetNamesAndValues();
    info.startingDirectory = dirResponse.GetValue(PIPE_PROPERTY_VALUE);
    info.aliases = std::move(aliasesResponse);
    info.colorTable = colorTableResponse.GetValue(PIPE_PROPERTY_VALUE);
    info.windowTitle = titleResponse.GetValue(PIPE_PROPERTY_VALUE);

    this->BackgroundStart(info);
}

// Thread that just sends commands to the other pipe server when a command is put in a queue
void Process::BackgroundSendCommands(HANDLE process)
{
    std::array<HANDLE, 3> handles = { this->commandEvent, this->disposeEvent, process };
    while (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
    {
        std::vector<Message> commands;
        {
            std::scoped_lock<std::mutex> pipeLock(this->processPipeMutex);
            std::scoped_lock<std::mutex> lock(this->commandsMutex);

            if (this->processPipe)
            {
                commands = std::move(this->commands);
            }

            ::ResetEvent(this->commandEvent);
        }

        this->SendCommands(commands);
    }
}

// Initialize a newly created process after pipes are connected
void Process::InitNewProcess(const ProcessStartInfo& info)
{
    if (info.colorTable.size())
    {
        Message message(PIPE_COMMAND_SET_COLOR_TABLE);
        message.SetValue(PIPE_PROPERTY_VALUE, info.colorTable);
        this->SendCommandAsync(std::move(message));
    }

    if (info.aliases.HasAnyName())
    {
        Message aliases = info.aliases;
        aliases.SetCommand(PIPE_COMMAND_SET_ALIASES);
        this->SendCommandAsync(std::move(aliases));
    }

    if (info.windowTitle.size())
    {
        Message message(PIPE_COMMAND_SET_TITLE);
        message.SetValue(PIPE_PROPERTY_VALUE, info.windowTitle);
        this->SendCommandAsync(std::move(message));
    }
}

// Adds a command to the queue to send to the other process. It may never be sent if the other process dies.
void Process::SendCommandAsync(Message&& command)
{
    // call from any thread

    std::scoped_lock<std::mutex> lock(this->commandsMutex);
    this->commands.push_back(std::move(command));
    ::SetEvent(this->commandEvent);
}

void Process::SendCommands(const std::vector<Message>& commands)
{
    for (const Message& command : commands)
    {
        Message response;
        bool status = false;
        {
            std::scoped_lock<std::mutex> lock(this->processPipeMutex);
            status = this->processPipe && this->processPipe.Transact(command, response);
        }

        if (status)
        {
            this->ResponseHandler(response);
        }
    }
}

// Blocks while a command is sent
bool Process::TransactCommand(const Message& command)
{
    Message result;
    return this->TransactCommand(command, result);
}

// Blocks while a command is sent
bool Process::TransactCommand(const Message& command, Message& response)
{
    std::scoped_lock<std::mutex> lock(this->processPipeMutex);
    return this->processPipe&& this->processPipe.Transact(command, response);
}

// Called after the BackgroundSendCommands thread is gone to flush any remaining commands
void Process::FlushRemainingCommands()
{
    while (true)
    {
        std::vector<Message> commands;
        {
            std::scoped_lock<std::mutex> lock(this->commandsMutex);
            commands = std::move(this->commands);
            ::ResetEvent(this->commandEvent);
        }

        if (commands.size())
        {
            this->SendCommands(commands);
        }
        else
        {
            break;
        }
    }
}

// Handles commands that come in from the other process through my pipe server
Message Process::CommandHandler(HANDLE process, const Message& input)
{
    assert(!App::IsMainThread());

    Message result;
    std::shared_ptr<Process> self = this->shared_from_this();
    std::wstring command = input.GetCommand();

    if (command == PIPE_COMMAND_PIPE_CREATED)
    {
        std::wstring name = input.GetValue(PIPE_PROPERTY_VALUE);
        Pipe info = Pipe::Connect(process, this->disposeEvent);

        std::scoped_lock<std::mutex> pipeLock(this->processPipeMutex);
        this->processPipe = std::move(info);
        {
            std::scoped_lock<std::mutex> commandLock(this->commandsMutex);
            ::SetEvent(this->commandEvent);
        }
    }
    else if (command == PIPE_COMMAND_WINDOW_CREATED)
    {
        std::wstring hwndString = input.GetValue(PIPE_PROPERTY_VALUE);
        const wchar_t* start = hwndString.c_str();
        wchar_t* end = nullptr;
        unsigned long long hwndSize = std::wcstoull(start, &end, 10);

        if (hwndSize && end == start + hwndString.size())
        {
            HWND hwnd = reinterpret_cast<HWND>(hwndSize);

            this->app->PostToMainThread([self, hwnd]()
                {
                    self->SetChildWindow(hwnd);
                });
        }
    }
    else if (command == PIPE_COMMAND_TITLE_CHANGED)
    {
        std::wstring title = input.GetValue(PIPE_PROPERTY_VALUE);

        this->app->PostToMainThread([self, title]()
            {
                self->processWindowTitle = title;
                self->app->OnProcessTitleChanged(self.get(), title);
            });
    }
    else if (command == PIPE_COMMAND_ENV_CHANGED)
    {
        std::wstring env = input.GetNamesAndValues();

        this->app->PostToMainThread([self, env]()
            {
                self->processEnv = env;
                self->app->OnProcessEnvChanged(self.get(), env);
            });
    }

    return result;
}

// Handles command responses that come from the other process after we send it a command
void Process::ResponseHandler(const Message& response)
{
    assert(!App::IsMainThread());

    std::shared_ptr<Process> self = this->shared_from_this();
    std::wstring command = response.GetResponse();

    if (command == PIPE_COMMAND_GET_EXE)
    {
        std::wstring path = response.GetValue(PIPE_PROPERTY_VALUE);

        this->app->PostToMainThread([self, path]()
            {
                self->processExe = path;
            });
    }
    else if (command == PIPE_COMMAND_GET_TITLE)
    {
        std::wstring title = response.GetValue(PIPE_PROPERTY_VALUE);

        this->app->PostToMainThread([self, title]()
            {
                self->processWindowTitle = title;
                self->app->OnProcessTitleChanged(self.get(), title);
            });
    }
    else if (command == PIPE_COMMAND_GET_ENV)
    {
        std::wstring env = response.GetNamesAndValues();

        this->app->PostToMainThread([self, env]()
            {
                self->processEnv = env;
                self->app->OnProcessEnvChanged(self.get(), env);
            });
    }
}
