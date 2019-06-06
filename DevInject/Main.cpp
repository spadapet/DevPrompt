#include "stdafx.h"
#include "Context/AppContext.h"
#include "Context/ConhostContext.h"
#include "Context/OwnerContext.h"
#include "Main.h"
#include "Utility.h"

static HMODULE module = nullptr;
static BaseContext* context = nullptr;

static void Initialize(HMODULE module);

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        ::Initialize(module);
        break;

    case DLL_PROCESS_DETACH:
        ::module = nullptr;
        break;

    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }

    return TRUE;
}

enum class ProcessType
{
    Unknown,
    OwnerApp,
    InjectedApp,
    InjectedConHost,
};

static ProcessType DetectProcessType()
{
    std::wstring file = DevInject::GetModuleFileName(nullptr);
    size_t slash = file.rfind('\\');

    if (slash != std::wstring::npos)
    {
        file = file.substr(slash + 1);

        if (!_wcsicmp(file.c_str(), L"conhost.exe"))
        {
            return ProcessType::InjectedConHost;
        }

        if (_wcsicmp(file.c_str(), L"DevPrompt.exe") && _wcsicmp(file.c_str(), L"DevPromptNetCore.exe"))
        {
            return ProcessType::InjectedApp;
        }
    }

    return ProcessType::OwnerApp;
}

void Initialize(HMODULE module)
{
    ::module = module;

    switch (::DetectProcessType())
    {
    case ProcessType::InjectedApp:
        ::context = new AppContext();
        break;

    case ProcessType::InjectedConHost:
        ::context = new ConhostContext();
        break;

    default:
        ::context = new OwnerContext();
        break;
    }

    ::context->Initialize();
}

HMODULE DevInject::Dispose()
{
    BaseContext* context = ::context;
    ::context = nullptr;

    if (context)
    {
        context->Dispose();
        delete context;
    }

    return ::module;
}

HMODULE DevInject::GetModule()
{
    return ::module;
}
