#pragma once

#include "DevPrompt_h.h"

struct ISetupInstance2;

class VisualStudioInstances : public IVisualStudioInstances
{
public:
    VisualStudioInstances(std::vector<Microsoft::WRL::ComPtr<ISetupInstance2>>&& instances);
    ~VisualStudioInstances();

    static Microsoft::WRL::ComPtr<IVisualStudioInstances> Create();

    // IUnknown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** obj) override;
    virtual ULONG __stdcall AddRef() override;
    virtual ULONG __stdcall Release() override;

    // IVisualStudioInstances
    virtual HRESULT __stdcall GetCount(int* count);
    virtual HRESULT __stdcall GetValue(int index, IVisualStudioInstance** value);

private:
    unsigned long refs;
    std::vector<Microsoft::WRL::ComPtr<ISetupInstance2>> instances;
};
