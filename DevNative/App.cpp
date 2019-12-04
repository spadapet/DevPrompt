#include "stdafx.h"
#include "App.h"
#include "ConsoleProcess.h"
#include "DevPrompt_h.h"
#include "Interop/ProcessInterop.h"
#include "Json/Persist.h"

static App* app = nullptr;
static const UINT_PTR WINDOWS_CHANGED_TIMER = 1;
static const UINT WINDOWS_CHANGED_TIMEOUT = 100;
static const UINT WM_USER_RUN_TASKS = WM_USER + 1;

App::App(IAppHost* host, bool elevated, HINSTANCE instance, HANDLE destructEvent)
    : host(host)
    , elevated(elevated)
    , instance(instance)
    , destructEvent(destructEvent)
    , mainThread(::GetCurrentThreadId())
    , messageWindow(nullptr)
    , shellMessage(::RegisterWindowMessage(L"SHELLHOOK"))
    , active(false)
    , processCount(0)
    , messageFont(nullptr)
    , messageFontDpi(0)
{
    assert(!::app);
    ::app = this;

    ::InitializeCriticalSection(&this->processCountCS);
    ::InitializeConditionVariable(&this->processCountZeroCV);
}

App::~App()
{
    this->Dispose();

    if (this->messageFont)
    {
        ::DeleteObject(this->messageFont);
        this->messageFont = nullptr;
    }

    ::DeleteCriticalSection(&this->processCountCS);

    if (this->destructEvent)
    {
        ::SetEvent(this->destructEvent);
    }

    assert(::app == this);
    ::app = nullptr;
}

void App::Initialize()
{
    assert(App::IsMainThread());

    WNDCLASSEX windowClass{};
    windowClass.cbSize = sizeof(windowClass);
    windowClass.hInstance = this->instance;
    windowClass.lpszClassName = L"App::Message";

    this->messageWindow = IWindowProc::Create(this, windowClass, 0, RECT{}, HWND_MESSAGE);

    ::RegisterShellHookWindow(this->messageWindow);
    ::RegisterApplicationRestart(this->elevated ? L"/admin /restarted" : L"/restarted", RESTART_NO_CRASH | RESTART_NO_HANG);
    ::SetProcessShutdownParameters(0x300, 0); // shut down before hosted processes

    this->UpdateEnvironmentVariables();
}

void App::Dispose()
{
    assert(App::IsMainThread() || this->host == nullptr);

    this->DisposeMessageWindow();
    this->RunAllTasks();
    this->DisposeAllProcessesAndWait();

    this->host.Reset();
}

App* App::Get()
{
    return ::app;
}

bool App::IsMainThread()
{
    return ::app && ::app->mainThread == ::GetCurrentThreadId();
}

HINSTANCE App::GetInstance() const
{
    return this->instance;
}

IAppHost* App::GetHost() const
{
    assert(App::IsMainThread() && this->host);
    return this->host.Get();
}

App::Task::Task(App& app, std::function<void()>&& func)
    : app(app.shared_from_this())
    , func(std::move(func))
{
}

void App::Task::RunCallback(PTP_CALLBACK_INSTANCE instance, void* context)
{
    Task* task = reinterpret_cast<Task*>(context);
    task->func();
    delete task;
}

void App::PostBackgroundTask(std::function<void()>&& func)
{
    Task* task = new Task(*this, std::move(func));
    if (!::TrySubmitThreadpoolCallback(App::Task::RunCallback, task, nullptr))
    {
        assert(false);
    }
}

void App::PostToMainThread(std::function<void()>&& func, bool skipIfNoMainThread)
{
    bool runNow = false;
    bool postMessage = false;

    this->taskMutex.lock();

    if (this->messageWindow)
    {
        postMessage = this->tasks.empty();
        this->tasks.emplace_back(*this, std::move(func));
    }
    else
    {
        // Can't post to main thread
        runNow = !skipIfNoMainThread;
    }

    this->taskMutex.unlock();

    if (runNow)
    {
        func();
    }
    else if (postMessage)
    {
        ::PostMessage(this->messageWindow, WM_USER_RUN_TASKS, 0, 0);
    }
}

void App::AddListener(IAppListener* obj)
{
    auto i = std::find(this->listeners.begin(), this->listeners.end(), nullptr);
    if (i != this->listeners.end())
    {
        *i = obj;
    }
    else
    {
        this->listeners.push_back(obj);
    }
}

void App::RemoveListener(IAppListener* obj)
{
    auto i = std::find(this->listeners.begin(), this->listeners.end(), obj);
    if (i != this->listeners.end())
    {
        *i = nullptr;
    }
}

HFONT App::GetMessageFont(HWND hwnd)
{
    UINT dpi = ::GetDpiForWindow(hwnd);
    if (dpi != this->messageFontDpi || !this->messageFont)
    {
        NONCLIENTMETRICS ncm;
        ncm.cbSize = sizeof(ncm);

        if (::SystemParametersInfoForDpi(SPI_GETNONCLIENTMETRICS, sizeof(ncm), &ncm, 0, dpi))
        {
            this->messageFont = ::CreateFontIndirect(&ncm.lfMessageFont);
        }

        if (!this->messageFont)
        {
            this->messageFont = (HFONT)::GetStockObject(DEFAULT_GUI_FONT);
        }
    }

    return this->messageFont;
}

void App::NotifyWindowDestroying(HWND hwnd)
{
    for (size_t i = 0; i < this->listeners.size(); i++)
    {
        if (this->listeners[i])
        {
            this->listeners[i]->OnWindowDestroying(hwnd);
        }
    }
}

static bool CanGrabWindow(HWND hwnd)
{
    wchar_t name[20];
    if (hwnd &&
        ::IsWindow(hwnd) &&
        ::IsWindowVisible(hwnd) &&
        !::GetParent(hwnd) &&
        ::GetClassName(hwnd, name, _countof(name)) == 18 &&
        !::wcsncmp(name, L"ConsoleWindowClass", 18))
    {
        return true;
    }

    return false;
}

void App::CheckPendingWindows()
{
    assert(App::IsMainThread());

    std::vector<HWND> hwnds;
    hwnds.reserve(this->pendingWindows.size());

    for (HWND hwnd : this->pendingWindows)
    {
        if (std::find(hwnds.begin(), hwnds.end(), hwnd) == hwnds.end() &&
            std::find(this->noAutoGrabWindows.begin(), this->noAutoGrabWindows.end(), hwnd) == this->noAutoGrabWindows.end() &&
            ::CanGrabWindow(hwnd))
        {
            hwnds.push_back(hwnd);
        }
    }

    this->pendingWindows.clear();

    for (HWND hwnd : hwnds)
    {
        DWORD procId;
        if (::GetWindowThreadProcessId(hwnd, &procId))
        {
            HANDLE hwndProcess = ::OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, procId);
            if (hwndProcess)
            {
                wchar_t path[MAX_PATH];
                int canGrab = 0;
                if (::GetProcessImageFileName(hwndProcess, path, _countof(path)) && SUCCEEDED(this->host->CanGrab(path, VARIANT_TRUE, &canGrab)) && canGrab)
                {
                    this->host->TrackEvent(L"Grab.AutoGrabProcess");
                    this->AttachProcess(nullptr, hwndProcess, canGrab == 2);
                }

                ::CloseHandle(hwndProcess);
            }
        }
    }
}

static void UpdateEnvironmentVariablesFromKey(HKEY key)
{
    DWORD valueCount;
    DWORD maxNameLength;
    DWORD maxValueLength;
    if (::RegQueryInfoKey(key, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, &valueCount, &maxNameLength, &maxValueLength, nullptr, nullptr) == ERROR_SUCCESS)
    {
        std::vector<wchar_t> nameBuffer;
        std::vector<BYTE> valueBuffer;

        nameBuffer.resize(static_cast<size_t>(maxNameLength) + 1, 0);
        valueBuffer.resize(maxValueLength, 0);

        for (DWORD i = 0; i < valueCount; i++)
        {
            DWORD type;
            DWORD nameBufferSize = static_cast<DWORD>(nameBuffer.size());
            DWORD valueBufferSize = static_cast<DWORD>(valueBuffer.size());
            if (::RegEnumValue(key, i, nameBuffer.data(), &nameBufferSize, nullptr, &type, valueBuffer.data(), &valueBufferSize) == ERROR_SUCCESS && type == REG_SZ)
            {
                const wchar_t* name = nameBuffer.data();
                const wchar_t* value = reinterpret_cast<const wchar_t*>(valueBuffer.data());
                ::SetEnvironmentVariable(name, *value ? value : nullptr);
            }
        }
    }
}

void App::UpdateEnvironmentVariables()
{
    assert(App::IsMainThread());

#ifdef _DEBUG
    // Make sure that XAML debugging still works
    const wchar_t* xamlSourceInfoName = L"ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO";
    wchar_t xamlSourceInfoBuffer[32];
    DWORD xamlSourceInfoBufferLen = ::GetEnvironmentVariable(xamlSourceInfoName, xamlSourceInfoBuffer, 32);

    if (!xamlSourceInfoBufferLen || xamlSourceInfoBufferLen > 32)
    {
        xamlSourceInfoBuffer[0] = L'\0';
    }
#endif

    void* env;
    if (::CreateEnvironmentBlock(&env, ::GetCurrentProcessToken(), FALSE))
    {
        ::SetEnvironmentStrings(reinterpret_cast<wchar_t*>(env));
        ::DestroyEnvironmentBlock(env);
    }

    // CreateEnvironmentBlock doesn't include the "Volatile Environment" for some reason
    HKEY envKey;
    if (::RegOpenKeyEx(HKEY_CURRENT_USER, L"Volatile Environment", 0, KEY_READ, &envKey) == ERROR_SUCCESS)
    {
        ::UpdateEnvironmentVariablesFromKey(envKey);

        DWORD sessionId;
        if (::ProcessIdToSessionId(::GetCurrentProcessId(), &sessionId))
        {
            std::wstring sessionString = std::to_wstring(sessionId);

            HKEY sessionKey;
            if (::RegOpenKeyEx(envKey, sessionString.c_str(), 0, KEY_READ, &sessionKey) == ERROR_SUCCESS)
            {
                ::UpdateEnvironmentVariablesFromKey(sessionKey);
                ::RegCloseKey(sessionKey);
            }

            ::RegCloseKey(envKey);
        }
    }

#ifdef _DEBUG
    if (xamlSourceInfoBuffer[0])
    {
        ::SetEnvironmentVariable(xamlSourceInfoName, xamlSourceInfoBuffer);
    }
#endif
}

// Called during App::Dispose
void App::DisposeMessageWindow()
{
    assert(App::IsMainThread());

    if (this->messageWindow)
    {
        // Don't allow new tasks to be posted while destroying the window
        std::scoped_lock<std::mutex> lock(this->taskMutex);

        ::DeregisterShellHookWindow(this->messageWindow);
        ::DestroyWindow(this->messageWindow);
        this->messageWindow = nullptr;
    }
}

// Called during App::Dispose
void App::DisposeAllProcessesAndWait()
{
    assert(App::IsMainThread());

    std::vector<std::shared_ptr<ConsoleProcess>> processes = this->processes;

    for (std::shared_ptr<ConsoleProcess>& process : processes)
    {
        process->Dispose();
    }

    assert(this->processes.empty());
    processes.clear();

    ::EnterCriticalSection(&this->processCountCS);

    while (this->processCount)
    {
        ::SleepConditionVariableCS(&this->processCountZeroCV, &this->processCountCS, INFINITE);
    }

    ::LeaveCriticalSection(&this->processCountCS);

    std::vector<HWND> processHostWindows = this->processHostWindows;

    for (HWND hwnd : processHostWindows)
    {
        this->DisposeProcessHostWindow(hwnd);
    }

    assert(this->processHostWindows.empty());
}

void App::RunAllTasks()
{
    assert(App::IsMainThread());

    this->taskMutex.lock();
    std::list<Task> tasks(std::move(this->tasks));
    this->taskMutex.unlock();

    for (Task& task : tasks)
    {
        task.func();
    }
}

static bool IsKeyPressed(int vk)
{
    return ::GetAsyncKeyState(vk) < 0;
}

HWND App::CreateProcessHostWindow(HWND parentWnd)
{
    assert(App::IsMainThread() && parentWnd);

    WNDCLASSEX windowClass{};
    windowClass.cbSize = sizeof(windowClass);
    windowClass.hInstance = this->GetInstance();
    windowClass.lpszClassName = L"App::Host";
    windowClass.hCursor = ::LoadCursor(nullptr, IDC_ARROW);
    windowClass.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_BTNFACE + 1);

    RECT rect;
    ::GetClientRect(parentWnd, &rect);
    HWND hwnd = IWindowProc::Create(this, windowClass, WS_CHILD | WS_CLIPCHILDREN, rect, parentWnd);
    if (hwnd)
    {
        this->processHostWindows.push_back(hwnd);
    }

    assert(hwnd);
    return hwnd;
}

void App::DisposeProcessHostWindow(HWND hwnd)
{
    assert(App::IsMainThread());

    auto i = std::find(this->processHostWindows.begin(), this->processHostWindows.end(), hwnd);
    assert(i != this->processHostWindows.end());

    if (i != this->processHostWindows.end())
    {
        this->NotifyWindowDestroying(hwnd);
        this->processHostWindows.erase(i);
        ::DestroyWindow(hwnd);
    }
}

void App::Activate()
{
    assert(App::IsMainThread());

    this->active = true;
}

void App::Deactivate()
{
    assert(App::IsMainThread());

    this->active = false;
}

bool App::IsActive() const
{
    assert(App::IsMainThread());

    return this->active;
}

void App::ActivateProcessHostWindow(HWND hwnd)
{
    assert(App::IsMainThread());
}

void App::DeactivateProcessHostWindow(HWND hwnd)
{
    assert(App::IsMainThread());
}

void App::ShowProcessHostWindow(HWND hwnd)
{
    assert(App::IsMainThread());

    ::ShowWindow(hwnd, SW_SHOW);
}

void App::HideProcessHostWindow(HWND hwnd)
{
    assert(App::IsMainThread());

    ::ShowWindow(hwnd, SW_HIDE);
}

void App::ProcessHostWindowDpiChanged(HWND hwnd)
{
    std::shared_ptr<App> self = this->shared_from_this();

    this->PostToMainThread([self, hwnd]()
    {
        for (std::shared_ptr<ConsoleProcess>& i : self->processes)
        {
            if (i->GetHostWindow() && ::GetParent(i->GetHostWindow()) == hwnd)
            {
                i->SendDpiChanged();
            }
        }
    }, true);
}

HWND App::RunProcess(HWND processHostWindow, const Json::Dict& info)
{
    assert(App::IsMainThread());

    std::shared_ptr<ConsoleProcess> process = std::make_shared<ConsoleProcess>(*this);
    process->Initialize(processHostWindow);
    assert(process->GetHostWindow());

    this->processes.push_back(process);
    if (process->Start(info))
    {
        Microsoft::WRL::ComPtr<IProcess> processInterop = new ProcessInterop(this, process->GetHostWindow());
        this->host->OnProcessOpening(processInterop.Get(), VARIANT_TRUE, nullptr);
    }
    else
    {
        ::DestroyWindow(process->GetHostWindow());
    }

    return process->GetHostWindow();
}

HWND App::CloneProcess(HWND processHostWindow, HWND hwnd)
{
    assert(App::IsMainThread());

    std::shared_ptr<ConsoleProcess> process;
    std::shared_ptr<ConsoleProcess> firstProcess = this->FindProcess(hwnd);

    if (firstProcess)
    {
        process = std::make_shared<ConsoleProcess>(*this);
        process->Initialize(processHostWindow);
        assert(process->GetHostWindow());

        this->processes.push_back(process);
        if (process->Clone(firstProcess))
        {
            Microsoft::WRL::ComPtr<IProcess> processInterop = new ProcessInterop(this, process->GetHostWindow());
            this->host->OnProcessOpening(processInterop.Get(), VARIANT_TRUE, nullptr);
        }
        else
        {
            ::DestroyWindow(process->GetHostWindow());
        }
    }

    assert(process);
    return process ? process->GetHostWindow() : nullptr;
}

HWND App::AttachProcess(HWND processHostWindow, HANDLE handle, bool activate)
{
    assert(App::IsMainThread());

    if (!processHostWindow)
    {
        processHostWindow = this->processHostWindows.size() ? this->processHostWindows.back() : nullptr;
        if (!processHostWindow)
        {
            return nullptr;
        }
    }

    std::shared_ptr<ConsoleProcess> process;
    std::shared_ptr<ConsoleProcess> firstProcess = handle ? this->FindProcess(::GetProcessId(handle)) : nullptr;

    if (handle && !firstProcess && ::GetProcessId(handle) != ::GetCurrentProcessId())
    {
        process = std::make_shared<ConsoleProcess>(*this);
        process->Initialize(processHostWindow);
        assert(process->GetHostWindow());

        this->processes.push_back(process);
        if (process->Attach(handle))
        {
            wchar_t path[MAX_PATH];
            const wchar_t* passPath = ::GetProcessImageFileName(handle, path, _countof(path)) ? path : nullptr;

            Microsoft::WRL::ComPtr<IProcess> processInterop = new ProcessInterop(this, process->GetHostWindow());
            this->host->OnProcessOpening(processInterop.Get(), activate ? VARIANT_TRUE : VARIANT_FALSE, passPath);
        }
        else
        {
            ::DestroyWindow(process->GetHostWindow());
        }
    }

    assert(process);
    return process ? process->GetHostWindow() : nullptr;
}

std::shared_ptr<ConsoleProcess> App::FindProcess(HWND hwnd)
{
    assert(App::IsMainThread());

    for (std::shared_ptr<ConsoleProcess>& i : this->processes)
    {
        if (i->GetHostWindow() == hwnd)
        {
            return i;
        }
    }

    return nullptr;
}

std::shared_ptr<ConsoleProcess> App::FindProcess(DWORD procId)
{
    assert(App::IsMainThread());

    for (std::shared_ptr<ConsoleProcess>& i : this->processes)
    {
        if (i->GetProcessId() == procId)
        {
            return i;
        }
    }

    return nullptr;
}

std::shared_ptr<ConsoleProcess> App::FindActiveProcess()
{
    assert(App::IsMainThread());

    for (std::shared_ptr<ConsoleProcess>& i : this->processes)
    {
        if (i->IsActive())
        {
            return i;
        }
    }

    return nullptr;
}

void App::ActivateProcess(HWND hwnd)
{
    std::shared_ptr<ConsoleProcess> process = this->FindProcess(hwnd);
    if (process)
    {
        ::SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        process->Activate();
    }
}

void App::DeactivateProcess(HWND hwnd)
{
    std::shared_ptr<ConsoleProcess> process = this->FindProcess(hwnd);
    if (process)
    {
        process->Deactivate();
    }
}

void App::DisposeProcess(HWND hwnd)
{
    std::shared_ptr<ConsoleProcess> process = this->FindProcess(hwnd);
    if (process)
    {
        process->Dispose();
    }
}

void App::DetachProcess(HWND hwnd)
{
    std::shared_ptr<ConsoleProcess> process = this->FindProcess(hwnd);
    if (process)
    {
        process->Detach();
    }
}

void App::SendProcessSystemCommand(HWND hwnd, UINT id)
{
    std::shared_ptr<ConsoleProcess> process = this->FindProcess(hwnd);
    if (process)
    {
        process->SendSystemCommand(id);
    }
}

std::wstring App::GetProcessState(HWND hwnd)
{
    std::shared_ptr<ConsoleProcess> process = this->FindProcess(hwnd);
    return process ? process->GetProcessState() : std::wstring();
}

void App::OnProcessCreated(ConsoleProcess* process)
{
    ::EnterCriticalSection(&this->processCountCS);

    this->processCount++;

    ::LeaveCriticalSection(&this->processCountCS);
}

void App::OnProcessDestroyed(ConsoleProcess* process)
{
    ::EnterCriticalSection(&this->processCountCS);

    bool allGone = !(--this->processCount);

    ::LeaveCriticalSection(&this->processCountCS);

    if (allGone)
    {
        ::WakeAllConditionVariable(&this->processCountZeroCV);
    }
}

void App::OnProcessClosing(ConsoleProcess* process)
{
    assert(App::IsMainThread());

    for (auto i = this->processes.begin(); i != this->processes.end(); i++)
    {
        std::shared_ptr<ConsoleProcess> pi = *i;
        if (pi.get() == process)
        {
            Microsoft::WRL::ComPtr<IProcess> processInterop = new ProcessInterop(this, pi->GetHostWindow());
            this->processes.erase(i);
            this->host->OnProcessClosing(processInterop.Get());
            this->NotifyWindowDestroying(pi->GetHostWindow());
            break;
        }
    }
}

void App::OnProcessEnvChanged(ConsoleProcess* process, const Json::Dict& env)
{
    Microsoft::WRL::ComPtr<IProcess> processInterop = new ProcessInterop(this, process->GetHostWindow());
    this->host->OnProcessEnvChanged(processInterop.Get(), Json::WriteNameValuePairs(env, L'\n').c_str());
}

void App::OnProcessTitleChanged(ConsoleProcess* process, const std::wstring& title)
{
    Microsoft::WRL::ComPtr<IProcess> processInterop = new ProcessInterop(this, process->GetHostWindow());
    this->host->OnProcessTitleChanged(processInterop.Get(), title.c_str());
}

struct GrabProcessData
{
    std::shared_ptr<App> app;
    std::wstringstream result;
};

BOOL App::FindProcessToGrab(HWND hwnd, LPARAM lp)
{
    GrabProcessData& data = *reinterpret_cast<GrabProcessData*>(lp);

    DWORD procId;
    if (::CanGrabWindow(hwnd) && ::GetWindowThreadProcessId(hwnd, &procId))
    {
        HANDLE hwndProcess = ::OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, procId);
        if (hwndProcess)
        {
            wchar_t path[MAX_PATH];
            int canGrab = 0;
            if (::GetProcessImageFileName(hwndProcess, path, _countof(path)))
            {
                wchar_t text[32];
                ::GetWindowText(hwnd, text, 32);

                const wchar_t* name = ::wcsrchr(path, '\\');
                name = name ? name + 1 : path;

                data.result << L"[" << procId << L"] " << name << ", '" << text << "'\n";
            }

            ::CloseHandle(hwndProcess);
        }
    }

    return TRUE;
}

std::wstring App::GetGrabProcesses()
{
    // Can be called from any thread
    GrabProcessData data;
    data.app = this->shared_from_this();

    std::wstring str;
    ::EnumDesktopWindows(nullptr, &App::FindProcessToGrab, reinterpret_cast<LPARAM>(&data));

    return data.result.str();
}

void App::GrabProcess(DWORD id)
{
    HANDLE process = ::OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, id);
    if (process)
    {
        this->AttachProcess(nullptr, process, true);

        ::CloseHandle(process);
    }
}

void App::NoAutoGrabWindow(HWND hwnd)
{
    assert(App::IsMainThread());

    if (std::find(this->noAutoGrabWindows.begin(), this->noAutoGrabWindows.end(), hwnd) == this->noAutoGrabWindows.end())
    {
        this->noAutoGrabWindows.push_back(hwnd);
    }
}

void App::MainWindowProc(HWND hwnd, int msg, WPARAM wp, LPARAM lp)
{
    switch (msg)
    {
    case WM_SETTINGCHANGE:
        this->UpdateEnvironmentVariables();
        break;
    }
}

LRESULT App::WindowProc(HWND hwnd, UINT message, WPARAM wp, LPARAM lp)
{
    assert(App::IsMainThread());

    if (hwnd == this->messageWindow)
    {
        if (message == this->shellMessage)
        {
            HWND shellHwnd = reinterpret_cast<HWND>(lp);
            switch (wp)
            {
            case HSHELL_WINDOWCREATED:
                this->pendingWindows.push_back(shellHwnd);
                ::SetTimer(hwnd, ::WINDOWS_CHANGED_TIMER, ::WINDOWS_CHANGED_TIMEOUT, nullptr);
                break;

            case HSHELL_WINDOWDESTROYED:
                auto i = std::find(this->pendingWindows.begin(), this->pendingWindows.end(), shellHwnd);
                if (i != this->pendingWindows.end())
                {
                    this->pendingWindows.erase(i);
                }

                i = std::find(this->noAutoGrabWindows.begin(), this->noAutoGrabWindows.end(), shellHwnd);
                if (i != this->noAutoGrabWindows.end())
                {
                    this->noAutoGrabWindows.erase(i);
                }
                break;
            }
        }
        else switch (message)
        {
        case WM_USER_RUN_TASKS:
            this->RunAllTasks();
            return 0;

        case WM_TIMER:
            // Make sure all processes have a known process ID before continuing
            if (wp == ::WINDOWS_CHANGED_TIMER && this->FindProcess((DWORD)0) == nullptr)
            {
                ::KillTimer(hwnd, ::WINDOWS_CHANGED_TIMER);
                this->CheckPendingWindows();
                return 0;
            }
            break;
        }
    }
    // It's a process host window
    else switch (message)
    {
    case WM_SIZE:
        WindowProc::ResizeChildren(hwnd);
        break;

    case WM_SETFOCUS:
        WindowProc::FocusFirstChild(hwnd);
        break;

    case WM_PAINT:
        WindowProc::PaintMessage(hwnd, this->GetMessageFont(hwnd), L"Use the 'File' menu to start a command prompt.");
        break;

    case WM_DPICHANGED_AFTERPARENT:
        this->ProcessHostWindowDpiChanged(hwnd);
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
