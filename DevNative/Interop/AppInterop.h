#pragma once

#include "DevPrompt_h.h"

class App;

class AppInterop : public IApp
{
public:
    AppInterop(IAppHost* host, bool elevated, HINSTANCE instance);
    ~AppInterop();

    // IUnknown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** obj) override;
    virtual ULONG __stdcall AddRef() override;
    virtual ULONG __stdcall Release() override;

    // IApp
    virtual HRESULT __stdcall Dispose() override;
    virtual HRESULT __stdcall Activate() override;
    virtual HRESULT __stdcall Deactivate() override;
    virtual HRESULT __stdcall GetGrabProcesses(BSTR* processes) override;
    virtual HRESULT __stdcall GrabProcess(int id) override;
    virtual HRESULT __stdcall CreateProcessHostWindow(HWND parentHwnd, IProcessHost** obj) override;
    virtual HRESULT __stdcall MainWindowProc(HWND hwnd, int msg, WPARAM wp, LPARAM lp) override;
    virtual HRESULT __stdcall GetDownloadsFolder(BSTR* processes) override;

private:
    unsigned long refs;
    std::shared_ptr<App> app;
    HANDLE appDestructEvent;
};
