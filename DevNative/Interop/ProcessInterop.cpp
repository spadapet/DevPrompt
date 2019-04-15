#include "stdafx.h"
#include "App.h"
#include "ProcessInterop.h"

ProcessInterop::ProcessInterop(App* app, HWND hwnd)
    : app(app->shared_from_this())
    , hwnd(hwnd)
    , refs(0)
{
    app->AddListener(this);
}

ProcessInterop::~ProcessInterop()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app)
    {
        app->RemoveListener(this);
    }
}

HRESULT ProcessInterop::QueryInterface(REFIID riid, void** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }
    else if (riid == __uuidof(IUnknown))
    {
        *obj = static_cast<IUnknown*>(this);
    }
    else if (riid == __uuidof(IProcess))
    {
        *obj = static_cast<IProcess*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

ULONG ProcessInterop::AddRef()
{
    return ::InterlockedIncrement(&this->refs);
}

ULONG ProcessInterop::Release()
{
    ULONG refs = ::InterlockedDecrement(&this->refs);
    if (!refs)
    {
        delete this;
    }

    return refs;
}

HRESULT ProcessInterop::Dispose()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->DisposeProcess(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::Detach()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->DetachProcess(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::Activate()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->ActivateProcess(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::Deactivate()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->DeactivateProcess(this->hwnd);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::GetWindow(HWND* hwnd)
{
    if (!hwnd)
    {
        return E_INVALIDARG;
    }

    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        *hwnd = this->hwnd;
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::GetState(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        std::wstring str = app->GetProcessState(this->hwnd);
        *value = ::SysAllocStringLen(str.c_str(), static_cast<UINT>(str.size()));
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::Focus()
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

HRESULT ProcessInterop::SystemCommandDefaults()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->SendProcessSystemCommand(this->hwnd, SC_CONHOST_DEFAULTS);
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT ProcessInterop::SystemCommandProperties()
{
    std::shared_ptr<App> app = this->app.lock();
    if (app && this->hwnd)
    {
        app->SendProcessSystemCommand(this->hwnd, SC_CONHOST_PROPERTIES);
        return S_OK;
    }

    return E_UNEXPECTED;
}

void ProcessInterop::OnWindowDestroying(HWND hwnd)
{
    if (hwnd == this->hwnd)
    {
        this->hwnd = nullptr;
    }
}
