#pragma once

#include "App.h"
#include "DevPrompt_h.h"

class ProcessInterop : public IProcess, public IAppListener
{
public:
    ProcessInterop(App* app, HWND hwnd);
    ~ProcessInterop();

    // IUnknown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** obj) override;
    virtual ULONG __stdcall AddRef() override;
    virtual ULONG __stdcall Release() override;

    // IProcessHost
    virtual HRESULT __stdcall Dispose() override;
    virtual HRESULT __stdcall Detach() override;
    virtual HRESULT __stdcall Activate() override;
    virtual HRESULT __stdcall Deactivate() override;
    virtual HRESULT __stdcall GetWindow(HWND* hwnd) override;
    virtual HRESULT __stdcall GetWindowTitle(BSTR* value) override;
    virtual HRESULT __stdcall GetExe(BSTR* value) override;
    virtual HRESULT __stdcall GetEnv(BSTR* value) override;
    virtual HRESULT __stdcall GetAliases(BSTR* value) override;
    virtual HRESULT __stdcall GetCurrentDirectory(BSTR* value) override;
    virtual HRESULT __stdcall GetColorTable(BSTR* value) override;
    virtual HRESULT __stdcall SetColorTable(const wchar_t* value) override;
    virtual HRESULT __stdcall Focus() override;
    virtual HRESULT __stdcall SystemCommandDefaults() override;
    virtual HRESULT __stdcall SystemCommandProperties() override;

    // IAppListener
    virtual void OnWindowDestroying(HWND hwnd) override;

private:
    unsigned long refs;
    std::weak_ptr<App> app;
    HWND hwnd;
};
