#include "stdafx.h"
#include "CommandHandler.h"
#include "Main.h"

static Message HandleGetEnv(const Message& input)
{
    Message result = input.CreateResponse();

    wchar_t* env = ::GetEnvironmentStrings();
    result.ParseNameValuePairs(env, '\0');
    ::FreeEnvironmentStrings(env);

    return result;
}

static Message HandleGetDir(const Message& input)
{
    Message result = input.CreateResponse();

    wchar_t value[MAX_PATH];
    if (::GetCurrentDirectory(_countof(value), value))
    {
        result.SetValue(PIPE_PROPERTY_VALUE, value);
    }

    return result;
}

static Message HandleGetExe(const Message& input)
{
    Message result = input.CreateResponse();

    wchar_t value[MAX_PATH];
    if (::GetModuleFileName(nullptr, value, _countof(value)))
    {
        result.SetValue(PIPE_PROPERTY_VALUE, value);
    }

    return result;
}

static Message HandleGetTitle(const Message& input)
{
    Message result = input.CreateResponse();

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
    Message result = input.CreateResponse();

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
                result.ParseNameValuePairs(aliasBuffer.data(), '\0', [cur](const std::wstring & s)
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
    for (const std::wstring& name : input.GetNames())
    {
        size_t pipe = name.find(L'|');
        if (pipe != std::wstring::npos)
        {
            std::wstring exeName = name.substr(0, pipe);
            std::wstring aliasName = name.substr(pipe + 1);
            const std::wstring& aliasValue = input.GetValue(name);

            ::AddConsoleAlias(
                const_cast<wchar_t*>(aliasName.c_str()),
                const_cast<wchar_t*>(aliasValue.c_str()),
                const_cast<wchar_t*>(exeName.c_str()));
        }
    }

    return input.CreateResponse();
}

static Message HandleSetTitle(const Message& input)
{
    std::wstring title = input.GetValue(PIPE_PROPERTY_VALUE);
    if (title.size())
    {
        ::SetConsoleTitle(title.c_str());
    }

    return input.CreateResponse();
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

    return input.CreateResponse();
}

static Message HandleGetColorTable(const Message& input)
{
    Message result = input.CreateResponse();

    HANDLE handle = ::GetStdHandle(STD_OUTPUT_HANDLE);
    CONSOLE_SCREEN_BUFFER_INFOEX info;
    info.cbSize = sizeof(info);

    if (::GetConsoleScreenBufferInfoEx(handle, &info))
    {
        std::wstringstream str;
        str << L"indexes=" << static_cast<unsigned int>(info.wAttributes & 0xFF) << L"\n";

        for (size_t i = 0; i < _countof(info.ColorTable); i++)
        {
            str << i << L"=" << info.ColorTable[i] << L"\n";
        }

        result.SetValue(PIPE_PROPERTY_VALUE, str.str());
    }

    return result;
}

static Message HandleSetColorTable(const Message& input)
{
    HANDLE handle = ::GetStdHandle(STD_OUTPUT_HANDLE);
    CONSOLE_SCREEN_BUFFER_INFOEX info;
    info.cbSize = sizeof(info);

    if (::GetConsoleScreenBufferInfoEx(handle, &info))
    {
        Message values;
        std::wstring value = input.GetValue(PIPE_PROPERTY_VALUE);
        values.ParseNameValuePairs(value.c_str(), '\n');

        std::wstring str = values.GetValue(L"indexes");
        if (str.size())
        {
            unsigned long value = std::wcstoul(str.c_str(), nullptr, 10);
            if (value)
            {
                info.wAttributes = (info.wAttributes & 0xFF00) | static_cast<WORD>(value & 0xFF);
            }
        }

        for (size_t i = 0; i < _countof(info.ColorTable); i++)
        {
            str = values.GetValue(std::to_wstring(i));
            if (str.size())
            {
                unsigned long value = std::wcstoul(str.c_str(), nullptr, 10);
                if (value)
                {
                    info.ColorTable[i] = value;
                }
            }
        }

        ::SetConsoleScreenBufferInfoEx(handle, &info);
    }

    return input.CreateResponse();
}

static Message SetConsoleWindowDpi(const Message& input)
{
    HWND hwnd = ::GetConsoleWindow();
    if (hwnd)
    {
        RECT rect;
        HWND parent = ::GetParent(hwnd);
        if (parent && ::GetClientRect(parent, &rect))
        {
            UINT dpi = ::GetDpiForWindow(hwnd);
            WPARAM wp = MAKEWPARAM(dpi, dpi);
            LPARAM lp = reinterpret_cast<LPARAM>(&rect);

            ::SendMessage(hwnd, WM_DPICHANGED, wp, lp);

            DevInject::CheckConsoleWindowSize(true);
        }
    }

    return input.CreateResponse();
}

// Handles commands comming in from the owner app
Message DevInject::CommandHandler(const Message& input)
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
    else if (command == PIPE_COMMAND_GET_COLOR_TABLE)
    {
        result = ::HandleGetColorTable(input);
    }
    else if (command == PIPE_COMMAND_SET_COLOR_TABLE)
    {
        result = ::HandleSetColorTable(input);
    }
    else if (command == PIPE_COMMAND_CHECK_WINDOW_SIZE)
    {
        DevInject::CheckConsoleWindowSize(false);
        result = input.CreateResponse();
    }
    else if (command == PIPE_COMMAND_CHECK_WINDOW_DPI)
    {
        result = ::SetConsoleWindowDpi(input);
    }
    else if (command == PIPE_COMMAND_ACTIVATED)
    {
        DevInject::CheckConsoleWindowSize(false);
        result = input.CreateResponse();
    }

    return result;
}
