#include "stdafx.h"
#include "VisualStudioInstance.h"
#include "VisualStudioInstances.h"

VisualStudioInstances::VisualStudioInstances(std::vector<Microsoft::WRL::ComPtr<ISetupInstance2>>&& instances)
    : instances(std::move(instances))
    , refs(0)
{
}

VisualStudioInstances::~VisualStudioInstances()
{
}

Microsoft::WRL::ComPtr<IVisualStudioInstances> VisualStudioInstances::Create()
{
    std::vector<Microsoft::WRL::ComPtr<ISetupInstance2>> instances;

    Microsoft::WRL::ComPtr<IClassFactory> factory;
    if (SUCCEEDED(::CoGetClassObject(__uuidof(SetupConfiguration), CLSCTX_INPROC_SERVER, nullptr, IID_IClassFactory, reinterpret_cast<void**>(factory.GetAddressOf()))))
    {
        Microsoft::WRL::ComPtr<ISetupConfiguration> config;
        Microsoft::WRL::ComPtr<IEnumSetupInstances> enumInstances;

        if (SUCCEEDED(factory->CreateInstance(nullptr, __uuidof(ISetupConfiguration), reinterpret_cast<void**>(config.GetAddressOf()))) &&
            SUCCEEDED(config->EnumInstances(enumInstances.GetAddressOf())))
        {
            for (Microsoft::WRL::ComPtr<ISetupInstance> instance; enumInstances->Next(1, instance.GetAddressOf(), nullptr) == S_OK; instance.Reset())
            {
                Microsoft::WRL::ComPtr<ISetupInstance2> instance2;
                if (SUCCEEDED(instance.As(&instance2)))
                {
                    instances.push_back(instance2);
                }
            }
        }
    }

    return new VisualStudioInstances(std::move(instances));
}

HRESULT VisualStudioInstances::QueryInterface(REFIID riid, void** obj)
{
    if (!obj)
    {
        return E_INVALIDARG;
    }
    else if (riid == __uuidof(IUnknown))
    {
        *obj = static_cast<IUnknown*>(this);
    }
    else if (riid == __uuidof(IVisualStudioInstances))
    {
        *obj = static_cast<IVisualStudioInstances*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

ULONG VisualStudioInstances::AddRef()
{
    return ::InterlockedIncrement(&this->refs);
}

ULONG VisualStudioInstances::Release()
{
    ULONG refs = ::InterlockedDecrement(&this->refs);
    if (!refs)
    {
        delete this;
    }

    return refs;
}

HRESULT VisualStudioInstances::GetCount(int* count)
{
    if (!count)
    {
        return E_INVALIDARG;
    }

    *count = static_cast<int>(this->instances.size());
    return S_OK;
}

HRESULT VisualStudioInstances::GetValue(int index, IVisualStudioInstance** value)
{
    if (index < 0 || !value)
    {
        return E_INVALIDARG;
    }

    size_t index2 = static_cast<size_t>(index);
    if (index2 >= this->instances.size())
    {
        return E_INVALIDARG;
    }

    *value = new VisualStudioInstance(this->instances[index2].Get());
    (*value)->AddRef();
    return S_OK;
}
