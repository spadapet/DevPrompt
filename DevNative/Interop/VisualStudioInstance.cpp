#include "stdafx.h"
#include "VisualStudioInstance.h"

static std::wstring GetString(ISetupInstance2* instance, HRESULT (ISetupInstance2::*func)(BSTR*))
{
    std::wstring result;

    BSTR bstr;
    if (SUCCEEDED((instance->*func)(&bstr)))
    {
        result = bstr;
        ::SysFreeString(bstr);
    }

    return result;
}

VisualStudioInstance::VisualStudioInstance(ISetupInstance2* instance)
    : refs(0)
{
    this->installationName = ::GetString(instance, &ISetupInstance2::GetInstallationName);
    this->instanceId = ::GetString(instance, &ISetupInstance2::GetInstanceId);
    this->installationPath = ::GetString(instance, &ISetupInstance2::GetInstallationPath);
    this->productPath = ::GetString(instance, &ISetupInstance2::GetProductPath);
    this->installationVersion = ::GetString(instance, &ISetupInstance2::GetInstallationVersion);

    Microsoft::WRL::ComPtr<ISetupPropertyStore> store;
    if (SUCCEEDED(instance->QueryInterface(__uuidof(ISetupPropertyStore), reinterpret_cast<void**>(store.GetAddressOf()))))
    {
        VARIANT var;
        ::VariantInit(&var);

        if (SUCCEEDED(store->GetValue(L"channelId", &var)) && V_VT(&var) == VT_BSTR)
        {
            this->channelId = var.bstrVal;
        }

        ::VariantClear(&var);
    }
}

VisualStudioInstance::~VisualStudioInstance()
{
}

HRESULT VisualStudioInstance::QueryInterface(REFIID riid, void** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }
    else if (riid == __uuidof(IUnknown))
    {
        *obj = static_cast<IUnknown*>(this);
    }
    else if (riid == __uuidof(IVisualStudioInstance))
    {
        *obj = static_cast<IVisualStudioInstance*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

ULONG VisualStudioInstance::AddRef()
{
    return ::InterlockedIncrement(&this->refs);
}

ULONG VisualStudioInstance::Release()
{
    ULONG refs = ::InterlockedDecrement(&this->refs);
    if (!refs)
    {
        delete this;
    }

    return refs;
}

HRESULT VisualStudioInstance::GetInstallationName(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    *value = ::SysAllocString(this->installationName.c_str());
    return S_OK;
}

HRESULT VisualStudioInstance::GetInstanceId(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    *value = ::SysAllocString(this->instanceId.c_str());
    return S_OK;
}

HRESULT VisualStudioInstance::GetInstallationPath(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    *value = ::SysAllocString(this->installationPath.c_str());
    return S_OK;
}

HRESULT VisualStudioInstance::GetProductPath(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    *value = ::SysAllocString(this->productPath.c_str());
    return S_OK;
}

HRESULT VisualStudioInstance::GetInstallationVersion(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    *value = ::SysAllocString(this->installationVersion.c_str());
    return S_OK;
}

HRESULT VisualStudioInstance::GetChannelId(BSTR* value)
{
    if (!value)
    {
        return E_INVALIDARG;
    }

    *value = ::SysAllocString(this->channelId.c_str());
    return S_OK;
}
