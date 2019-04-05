#pragma once

#include "DevPrompt_h.h"

struct ISetupInstance2;

class VisualStudioInstance : public IVisualStudioInstance
{
public:
    VisualStudioInstance(ISetupInstance2* instance);
    ~VisualStudioInstance();

    // IUnknown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** obj) override;
    virtual ULONG __stdcall AddRef() override;
    virtual ULONG __stdcall Release() override;

    // IVisualStudioInstance
    virtual HRESULT __stdcall GetInstallationName(BSTR* value);
    virtual HRESULT __stdcall GetInstanceId(BSTR* value);
    virtual HRESULT __stdcall GetInstallationPath(BSTR* value);
    virtual HRESULT __stdcall GetProductPath(BSTR* value);
    virtual HRESULT __stdcall GetInstallationVersion(BSTR* value);
    virtual HRESULT __stdcall GetChannelId(BSTR* value);

private:
    unsigned long refs;
    std::wstring installationName;
    std::wstring instanceId;
    std::wstring installationPath;
    std::wstring productPath;
    std::wstring installationVersion;
    std::wstring channelId;
};
