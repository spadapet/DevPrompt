#include "stdafx.h"
#include "App.h"
#include "AppInterop.h"
#include "ProcessHostInterop.h"

AppInterop::AppInterop(IAppHost* host, HINSTANCE instance)
    : refs(0)
    , appDestructEvent(::CreateEvent(nullptr, TRUE, FALSE, nullptr))
{
    this->app = std::make_shared<App>(host, instance, this->appDestructEvent);
    this->app->Initialize();
}

AppInterop::~AppInterop()
{
    this->Dispose();

    ::CloseHandle(this->appDestructEvent);
}

HRESULT AppInterop::QueryInterface(REFIID riid, void** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }
    else if (riid == __uuidof(IUnknown))
    {
        *obj = static_cast<IUnknown*>(this);
    }
    else if (riid == __uuidof(IApp))
    {
        *obj = static_cast<IApp*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

ULONG AppInterop::AddRef()
{
    return ::InterlockedIncrement(&this->refs);
}

ULONG AppInterop::Release()
{
    ULONG refs = ::InterlockedDecrement(&this->refs);
    if (!refs)
    {
        delete this;
    }

    return refs;
}

HRESULT AppInterop::Dispose()
{
    if (this->app)
    {
        this->app->Dispose();
        this->app.reset();

        // The app will destruct when all background tasks are done running
        ::WaitForSingleObject(this->appDestructEvent, INFINITE);

        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT AppInterop::Activate()
{
    if (this->app)
    {
        this->app->Activate();
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT AppInterop::Deactivate()
{
    if (this->app)
    {
        this->app->Deactivate();
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT AppInterop::GetGrabProcesses(BSTR* processes)
{
    if (!processes)
    {
        return E_INVALIDARG;
    }

    *processes = nullptr;

    if (this->app)
    {
        std::wstring str = this->app->GetGrabProcesses();
        *processes = ::SysAllocStringLen(str.c_str(), static_cast<UINT>(str.length()));
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT AppInterop::GrabProcess(int id)
{
    if (this->app)
    {
        this->app->GrabProcess(static_cast<DWORD>(id));
        return S_OK;
    }

    return E_UNEXPECTED;
}

HRESULT AppInterop::CreateProcessHostWindow(HWND parentHwnd, IProcessHost** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }

    if (this->app)
    {
        HWND hwnd = this->app->CreateProcessHostWindow(parentHwnd);
        if (hwnd)
        {
            *obj = new ProcessHostInterop(this->app.get(), hwnd);
            (*obj)->AddRef();
            return S_OK;
        }

        return E_FAIL;
    }

    return E_UNEXPECTED;
}
