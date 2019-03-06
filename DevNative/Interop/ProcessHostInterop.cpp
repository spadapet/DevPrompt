#include "stdafx.h"
#include "App.h"
#include "Process2.h"
#include "ProcessInterop.h"
#include "ProcessHostInterop.h"

ProcessHostInterop::ProcessHostInterop(App* app, HWND hwnd)
    : app(app->weak_from_this())
    , hwnd(hwnd)
    , refs(0)
{
    app->AddListener(this);
}

ProcessHostInterop::~ProcessHostInterop()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app)
    {
        app->RemoveListener(this);
    }
}

HRESULT ProcessHostInterop::QueryInterface(REFIID riid, void** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }
    else if (riid == __uuidof(IUnknown))
    {
        *obj = static_cast<IUnknown*>(this);
    }
    else if (riid == __uuidof(IProcessHost))
    {
        *obj = static_cast<IProcessHost*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

ULONG ProcessHostInterop::AddRef()
{
    return ::InterlockedIncrement(&this->refs);
}

ULONG ProcessHostInterop::Release()
{
    ULONG refs = ::InterlockedDecrement(&this->refs);
    if (!refs)
    {
        delete this;
    }

    return refs;
}

HRESULT ProcessHostInterop::Dispose()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->DisposeProcessHostWindow(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::Activate()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->ActivateProcessHostWindow(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::Deactivate()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->DeactivateProcessHostWindow(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::GetWindow(HWND* hwnd)
{
    if (!hwnd)
    {
        return E_INVALIDARG;
    }

    if (this->hwnd)
    {
        *hwnd = this->hwnd;
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::DpiChanged(double oldScale, double newScale)
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->ProcessHostWindowDpiChanged(this->hwnd, oldScale, newScale);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::Focus()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        if (app->IsActive())
        {
            ::SetFocus(this->hwnd);
        }

        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::RunProcess(
    const wchar_t* executable,
    const wchar_t* arguments,
    const wchar_t* startingDirectory,
    IProcess** obj)
{
    return this->RestoreProcess(executable, arguments, startingDirectory, nullptr, nullptr, nullptr, obj);
}

HRESULT ProcessHostInterop::RestoreProcess(
    const wchar_t* executable,
    const wchar_t* arguments,
    const wchar_t* currentDirectory,
    const wchar_t* environment,
    const wchar_t* aliases,
    const wchar_t* windowTitle,
    IProcess** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }

    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        ProcessStartInfo info;
        info.executable = executable ? executable : L"";
        info.arguments = arguments ? arguments : L"";
        info.startingDirectory = currentDirectory ? currentDirectory : L"";
        info.environment = environment ? environment : L"";
        info.windowTitle = windowTitle ? windowTitle : L"";

        std::wstring aliasesBuffer = aliases ? aliases : L"";
        std::replace(aliasesBuffer.begin(), aliasesBuffer.end(), L'\n', L'\0');
        aliasesBuffer += L'\0'; // just in case
        info.aliases.ParseNameValuePairs(aliasesBuffer.c_str());

        HWND hwnd = app->RunProcess(this->hwnd, info);
        if (hwnd)
        {
            *obj = new ProcessInterop(app.get(), hwnd);
            (*obj)->AddRef();
            return S_OK;
        }

        return E_FAIL;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessHostInterop::CloneProcess(IProcess* process, IProcess** obj)
{
    if (!process || !obj)
    {
        return E_INVALIDARG;
    }

    HWND processHwnd = nullptr;
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd && SUCCEEDED(process->GetWindow(&processHwnd)) && processHwnd)
    {
        HWND hwnd = app->CloneProcess(this->hwnd, processHwnd);
        if (hwnd)
        {
            *obj = new ProcessInterop(app.get(), hwnd);
            (*obj)->AddRef();
            return S_OK;
        }

        return E_FAIL;
    }

    return E_UNEXPECTED;
}

void ProcessHostInterop::OnWindowDestroying(HWND hwnd)
{
    if (hwnd == this->hwnd)
    {
        this->hwnd = nullptr;
    }
}
