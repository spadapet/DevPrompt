#include "stdafx.h"
#include "Message.h"

namespace DevInject
{
    HMODULE Dispose();
    Message CommandHandler(const Message& input);
}

static Message HandleGetEnv(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    wchar_t* env = ::GetEnvironmentStrings();
    result.ParseNameValuePairs(env);
    ::FreeEnvironmentStrings(env);

    return result;
}

static Message HandleGetDir(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    wchar_t value[MAX_PATH];
    if (::GetCurrentDirectory(_countof(value), value))
    {
        result.SetValue(PIPE_PROPERTY_VALUE, value);
    }

    return result;
}

static Message HandleGetExe(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    wchar_t value[MAX_PATH];
    if (::GetModuleFileName(nullptr, value, _countof(value)))
    {
        result.SetValue(PIPE_PROPERTY_VALUE, value);
    }

    return result;
}

static Message HandleGetTitle(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    HWND hwnd = ::GetConsoleWindow();
    if (hwnd)
    {
        wchar_t value[1024];
        if (::GetWindowText(hwnd, value, _countof(value)))
        {
            result.SetValue(PIPE_PROPERTY_VALUE, value);
        }
    }

    return result;
}

static Message HandleGetAliases(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    DWORD exesLength = ::GetConsoleAliasExesLength();
    exesLength = exesLength * 2 + 2; // return value was wrong, it was wchar_t count divided by 2, rather than multiplied by 2

    std::vector<wchar_t> exesBuffer;
    exesBuffer.resize(exesLength, 0);

    if (::GetConsoleAliasExes(exesBuffer.data(), static_cast<DWORD>(exesBuffer.size() * sizeof(wchar_t))))
    {
        for (wchar_t* cur = exesBuffer.data(); *cur; cur = cur + std::wcslen(cur) + 1)
        {
            DWORD size = ::GetConsoleAliasesLength(cur);
            std::vector<wchar_t> aliasBuffer;
            aliasBuffer.resize((size + 4) / sizeof(wchar_t), 0);

            if (::GetConsoleAliases(aliasBuffer.data(), static_cast<DWORD>(size), cur))
            {
                result.ParseNameValuePairs(aliasBuffer.data(), [cur](const std::wstring & s)
                    {
                        return std::wstring(cur) + std::wstring(L"|") + s;
                    });
            }
        }
    }

    return result;
}

static Message HandleSetAliases(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    for (const std::wstring& name : input.GetNames())
    {
        size_t pipe = name.find(L'|');
        if (pipe != std::wstring::npos)
        {
            std::wstring exeName = name.substr(0, pipe);
            std::wstring aliasName = name.substr(pipe + 1);
            const std::wstring & aliasValue = input.GetValue(name);

            ::AddConsoleAlias(
                const_cast<wchar_t*>(aliasName.c_str()),
                const_cast<wchar_t*>(aliasValue.c_str()),
                const_cast<wchar_t*>(exeName.c_str()));
        }
    }

    return result;
}

static Message HandleSetTitle(const Message& input)
{
    Message result(input.GetCommand(), input.GetId(), true);

    std::wstring title = input.GetValue(PIPE_PROPERTY_VALUE);
    if (title.size())
    {
        ::SetConsoleTitle(title.c_str());
    }

    return result;
}

static DWORD __stdcall DetachThread(void*)
{
    ::FreeLibraryAndExitThread(DevInject::Dispose(), 0);
    return 0;
}

static Message HandleDetach(const Message& input)
{
    HANDLE thread = ::CreateThread(nullptr, 0, ::DetachThread, nullptr, 0, nullptr);
    if (thread)
    {
        ::CloseHandle(thread);
    }

    return Message(input.GetCommand(), input.GetId(), true);
}

// Handles commands comming in from the owner app
Message DevInject::CommandHandler(const Message & input)
{
    Message result;

    std::wstring command = input.GetCommand();
    if (command == PIPE_COMMAND_GET_ENV)
    {
        result = ::HandleGetEnv(input);
    }
    else if (command == PIPE_COMMAND_SET_TITLE)
    {
        result = ::HandleSetTitle(input);
    }
    else if (command == PIPE_COMMAND_GET_CURRENT_DIRECTORY)
    {
        result = ::HandleGetDir(input);
    }
    else if (command == PIPE_COMMAND_GET_EXE)
    {
        result = ::HandleGetExe(input);
    }
    else if (command == PIPE_COMMAND_GET_TITLE)
    {
        result = ::HandleGetTitle(input);
    }
    else if (command == PIPE_COMMAND_GET_ALIASES)
    {
        result = ::HandleGetAliases(input);
    }
    else if (command == PIPE_COMMAND_SET_ALIASES)
    {
        result = ::HandleSetAliases(input);
    }
    else if (command == PIPE_COMMAND_DETACH)
    {
        result = ::HandleDetach(input);
    }

    return result;
}
