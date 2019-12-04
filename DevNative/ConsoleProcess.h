#pragma once

#include "Json/Message.h"
#include "Pipe.h"
#include "WindowProc.h"

class App;

class ConsoleProcess : public std::enable_shared_from_this<ConsoleProcess>, public IWindowProc
{
public:
    ConsoleProcess(App& app);
    ~ConsoleProcess();

    void Initialize(HWND processHostWindow);
    void Dispose();
    void Detach();

    bool Attach(HANDLE process);
    bool Start(const Json::Dict& info);
    bool Clone(const std::shared_ptr<ConsoleProcess>& process);
    HWND GetHostWindow() const;
    DWORD GetProcessId() const;
    std::wstring GetProcessState();
    void SendDpiChanged();
    void SendSystemCommand(UINT id);

    void Activate();
    void Deactivate();
    bool IsActive();

    // IWindowProc
    virtual LRESULT WindowProc(HWND hwnd, UINT message, WPARAM wp, LPARAM lp) override;

private:
    void SetChildWindow(HWND hwnd, DWORD processId);
    HWND GetChildWindow() const;
    void PostDispose();
    void InjectConhost(HWND conhostHwnd);

    void BackgroundAttach(HANDLE process, HANDLE mainThread = nullptr, const Json::Dict* info = nullptr);
    void BackgroundStart(const Json::Dict& info);
    void BackgroundClone(const std::shared_ptr<ConsoleProcess>& process);
    void BackgroundSendCommands(HANDLE process);
    void BackgroundInjectConhost(HWND conhostHwnd);
    void InitNewProcess(const Json::Dict& info);

    Json::Dict HandleMessage(HANDLE process, const Json::Dict& input);
    Json::Dict HandleConhostMessage(HANDLE process, HWND conhostHwnd, const Json::Dict& input);
    void HandleResponse(const std::wstring& name, const Json::Dict& output);
    void HandleNewState(const Json::Dict& state);

    void SendMessageAsync(std::wstring&& name);
    void SendMessageAsync(Json::Dict&& input);
    bool TransactMessage(std::wstring&& name);
    bool TransactMessage(std::wstring&& name, Json::Dict& output);
    bool TransactMessage(const Json::Dict& input, Json::Dict& output);
    void SendMessages(HANDLE process, const std::vector<Json::Dict>& messages);
    void FlushRemainingMessages(HANDLE process);

    std::shared_ptr<App> app;
    std::thread backgroundThread;
    std::thread injectConhostThread;
    HANDLE disposeEvent;
    HWND hostWnd;
    DWORD processId;
    std::wstring processWindowTitle;
    std::wstring processEnv;

    std::mutex processPipeMutex;
    Pipe processPipe;

    HANDLE messageEvent;
    std::mutex messageMutex;
    std::vector<Json::Dict> messages;
};
