// Called Process2.h because Process.h conflicts with a Windows header (only started with v142 toolset)
#pragma once

#include "WindowProc.h"

#include "../DevInject/Message.h"
#include "../DevInject/Pipe.h"

class App;

struct ProcessStartInfo
{
    std::wstring executable;
    std::wstring arguments;
    std::wstring environment;
    std::wstring startingDirectory;
    std::wstring windowTitle;
    Message aliases;
};

class Process : public std::enable_shared_from_this<Process>, public IWindowProc
{
public:
    Process(App& app);
    ~Process();

    void Initialize(HWND processHostWindow);
    void Dispose();
    void Detach();

    bool Attach(HANDLE process);
    bool Start(const ProcessStartInfo& info);
    bool Clone(const std::shared_ptr<Process>& process);
    HWND GetHostWindow() const;
    DWORD GetProcessId() const;
    std::wstring GetProcessExe();
    std::wstring GetProcessWindowTitle();
    std::wstring GetProcessEnv();
    std::wstring GetProcessAliases();
    std::wstring GetProcessCurrentDirectory();
    void SendDpiChanged(double oldScale, double newScale);
    void SendSystemCommand(UINT id);

    void Activate();
    void Deactivate();
    bool IsActive();

    // IWindowProc
    virtual LRESULT WindowProc(HWND hwnd, UINT message, WPARAM wp, LPARAM lp) override;

private:
    void SetChildWindow(HWND hwnd);
    HWND GetChildWindow() const;
    void PostDispose();

    void BackgroundAttach(HANDLE process, HANDLE mainThread = nullptr, const ProcessStartInfo* info = nullptr);
    void BackgroundStart(const ProcessStartInfo& info);
    void BackgroundClone(const std::shared_ptr<Process>& process);
    void BackgroundSendCommands(HANDLE process);
    void InitNewProcess(const ProcessStartInfo& info);
    HMODULE Inject(HANDLE process);

    Message CommandHandler(HANDLE process, const Message& input);
    void SendCommandAsync(Message&& command);
    bool TransactCommand(const Message& command, Message& response);
    void SendCommands(const std::vector<Message>& commands);
    void FlushRemainingCommands();
    void ResponseHandler(const Message& response);

    std::shared_ptr<App> app;
    std::thread backgroundThread;
    HANDLE disposeEvent;
    HWND hostWnd;
    DWORD processId;
    std::wstring processExe;
    std::wstring processWindowTitle;
    std::wstring processEnv;

    std::mutex processPipeMutex;
    Pipe processPipe;

    HANDLE commandEvent;
    std::mutex commandsMutex;
    std::vector<Message> commands;
};
