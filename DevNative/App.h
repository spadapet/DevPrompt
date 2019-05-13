#pragma once

#include "Json/Dict.h"
#include "WindowProc.h"

class Process;
struct IAppHost;

struct IAppListener
{
    virtual void OnWindowDestroying(HWND hwnd) = 0;
};

class App : public std::enable_shared_from_this<App>, public IWindowProc
{
public:
    App(IAppHost* host, bool elevated, HINSTANCE instance, HANDLE destructEvent);
    ~App();

    void Initialize();
    void Dispose();
    void Activate();
    void Deactivate();
    bool IsActive() const;

    static App* Get();
    static bool IsMainThread();

    HINSTANCE GetInstance() const;
    void PostToMainThread(std::function<void()>&& func);
    void PostBackgroundTask(std::function<void()>&& func);
    void AddListener(IAppListener* obj);
    void RemoveListener(IAppListener* obj);
    HFONT GetMessageFont(HWND hwnd);
    void MainWindowProc(HWND hwnd, int msg, WPARAM wp, LPARAM lp);

    // Process host windows (basically a parent window for all process HWNDs)
    HWND CreateProcessHostWindow(HWND parentWnd);
    void DisposeProcessHostWindow(HWND hwnd);
    void ActivateProcessHostWindow(HWND hwnd);
    void DeactivateProcessHostWindow(HWND hwnd);
    void ShowProcessHostWindow(HWND hwnd);
    void HideProcessHostWindow(HWND hwnd);

    // Process functions, each process is identified by its HWND
    HWND RunProcess(HWND processHostWindow, const Json::Dict& info);
    HWND CloneProcess(HWND processHostWindow, HWND hwnd);
    HWND AttachProcess(HWND processHostWindow, HANDLE handle, bool activate);
    void ActivateProcess(HWND hwnd);
    void DeactivateProcess(HWND hwnd);
    void DisposeProcess(HWND hwnd);
    void DetachProcess(HWND hwnd);
    void SendProcessSystemCommand(HWND hwnd, UINT id);
    std::wstring GetProcessState(HWND hwnd);
    std::wstring GetGrabProcesses();
    void GrabProcess(DWORD id);
    void NoAutoGrabWindow(HWND hwnd);

    // Notifications from Process objects
    void OnProcessCreated(Process* process);
    void OnProcessDestroyed(Process* process);
    void OnProcessClosing(Process* process);
    void OnProcessEnvChanged(Process* process, const Json::Dict& env);
    void OnProcessTitleChanged(Process* process, const std::wstring& title);

    // IWindowProc
    virtual LRESULT WindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) override;

private:
    std::shared_ptr<Process> FindProcess(HWND hwnd);
    std::shared_ptr<Process> FindProcess(DWORD procId);
    std::shared_ptr<Process> FindActiveProcess();

    void UpdateEnvironmentVariables();
    void DisposeMessageWindow();
    void DisposeAllProcessesAndWait();
    void RunAllTasks();
    void NotifyWindowDestroying(HWND hwnd);
    void CheckPendingWindows();
    void ProcessHostWindowDpiChanged(HWND hwnd);
    static BOOL CALLBACK FindProcessToGrab(HWND hwnd, LPARAM lp);

    // Keyboard hook is needed because focus is usually in another process (the console process)
    void AddRefKeyboardHook();
    void ReleaseKeyboardHook();
    void DisposeKeyboardHook();
    bool HandleKeyboardInput(const RAWINPUT& ri);

    struct Task
    {
        Task(App& app, std::function<void()>&& func);

        static void __stdcall RunCallback(PTP_CALLBACK_INSTANCE instance, void* context);

        std::shared_ptr<App> app;
        std::function<void()> func;
    };

    Microsoft::WRL::ComPtr<IAppHost> host;
    HINSTANCE instance;
    HANDLE destructEvent;
    DWORD mainThread;
    HWND mainWindow;
    HWND messageWindow;
    UINT shellMessage;
    bool active;
    bool elevated;
    std::vector<IAppListener*> listeners;
    std::vector<HWND> pendingWindows;
    std::vector<HWND> noAutoGrabWindows;
    HFONT messageFont;
    UINT messageFontDpi;

    // Posted tasks
    std::mutex taskMutex;
    std::list<Task> tasks;

    // Processes
    std::vector<std::shared_ptr<Process>> processes;
    std::vector<HWND> processHostWindows;
    int processCount;
    CRITICAL_SECTION processCountCS;
    CONDITION_VARIABLE processCountZeroCV;

    // Keyboard
    int keyboardHookCount;
    std::vector<BYTE> rawInput;
    std::array<bool, 0xFF> keysPressed;
    bool pressingAlt;
};
