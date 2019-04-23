#pragma once

#include "App.h"
#include "DevPrompt_h.h"

class ProcessHostInterop : public IProcessHost, public IAppListener
{
public:
    ProcessHostInterop(App* app, HWND hwnd);
    ~ProcessHostInterop();

    // IUnknown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** obj) override;
    virtual ULONG __stdcall AddRef() override;
    virtual ULONG __stdcall Release() override;

    // IProcessHost
    virtual HRESULT __stdcall Dispose() override;
    virtual HRESULT __stdcall Activate() override;
    virtual HRESULT __stdcall Deactivate() override;
    virtual HRESULT __stdcall Show() override;
    virtual HRESULT __stdcall Hide() override;
    virtual HRESULT __stdcall GetWindow(HWND* hwnd) override;
    virtual HRESULT __stdcall DpiChanged(double oldScale, double newScale) override;
    virtual HRESULT __stdcall Focus() override;

    virtual HRESULT __stdcall RunProcess(
        const wchar_t* executable,
        const wchar_t* arguments,
        const wchar_t* startingDirectory,
        IProcess** obj) override;

    virtual HRESULT __stdcall RestoreProcess(const wchar_t* state, IProcess** obj) override;
    virtual HRESULT __stdcall CloneProcess(IProcess* process, IProcess** obj) override;

    // IAppListener
    void OnWindowDestroying(HWND hwnd) override;

private:
    unsigned long refs;
    std::weak_ptr<App> app;
    HWND hwnd;
};
