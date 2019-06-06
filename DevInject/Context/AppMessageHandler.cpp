#include "stdafx.h"
#include "Context/AppMessageHandler.h"
#include "Json/Persist.h"
#include "Main.h"
#include "Utility.h"

static Json::Value HandleGetDirectory()
{
    wchar_t value[MAX_PATH];
    if (::GetCurrentDirectory(_countof(value), value))
    {
        return Json::Value(std::wstring(value));
    }

    return Json::Value();
}

static Json::Value HandleGetEnvironment()
{
    wchar_t* env = ::GetEnvironmentStrings();
    Json::Value result = Json::Value(Json::ParseNameValuePairs(env, '\0'));;
    ::FreeEnvironmentStrings(env);

    return result;
}

static Json::Value HandleGetExecutable()
{
    return Json::Value(DevInject::GetModuleFileName(nullptr));
}

static Json::Value HandleGetTitle()
{
    HWND hwnd = ::GetConsoleWindow();
    if (hwnd)
    {
        wchar_t value[1024];
        if (::GetWindowText(hwnd, value, _countof(value)))
        {
            return Json::Value(std::wstring(value));
        }
    }

    return Json::Value();
}

static void HandleSetTitle(const Json::Value& value)
{
    if (value.IsString())
    {
        ::SetConsoleTitle(value.GetString().c_str());
    }
}

static Json::Value HandleGetAliases()
{
    DWORD exesLength = ::GetConsoleAliasExesLength();
    exesLength = exesLength * 2 + 2; // return value was wrong, it was wchar_t count divided by 2, rather than multiplied by 2

    std::vector<wchar_t> exesBuffer;
    exesBuffer.resize(exesLength, 0);

    Json::Dict dict;

    if (::GetConsoleAliasExes(exesBuffer.data(), static_cast<DWORD>(exesBuffer.size() * sizeof(wchar_t))))
    {
        for (wchar_t* cur = exesBuffer.data(); *cur; cur = cur + std::wcslen(cur) + 1)
        {
            DWORD size = ::GetConsoleAliasesLength(cur);
            std::vector<wchar_t> aliasBuffer;
            aliasBuffer.resize((size + 4) / sizeof(wchar_t), 0);

            if (::GetConsoleAliases(aliasBuffer.data(), static_cast<DWORD>(size), cur))
            {
                dict.Set(cur, Json::Value(Json::ParseNameValuePairs(aliasBuffer.data(), '\0')));
            }
        }
    }

    return Json::Value(std::move(dict));
}

static void HandleSetAliases(const Json::Value& value)
{
    if (!value.IsDict())
    {
        return;
    }

    for (const auto& i : value.GetDict())
    {
        if (i.second.IsDict())
        {
            const std::wstring& exeName = i.first;

            for (const auto& h : i.second.GetDict())
            {
                if (h.second.IsString())
                {
                    const std::wstring& aliasName = h.first;
                    const std::wstring& aliasValue = h.second.GetString();

                    ::AddConsoleAlias(
                        const_cast<wchar_t*>(aliasName.c_str()),
                        const_cast<wchar_t*>(aliasValue.c_str()),
                        const_cast<wchar_t*>(exeName.c_str()));
                }
            }
        }
    }
}

static Json::Value HandleGetColors()
{
    Json::Dict dict;

    HANDLE handle = ::GetStdHandle(STD_OUTPUT_HANDLE);
    CONSOLE_SCREEN_BUFFER_INFOEX info;
    info.cbSize = sizeof(info);

    if (::GetConsoleScreenBufferInfoEx(handle, &info))
    {
        dict.Set(L"indexes", Json::Value(std::to_wstring(info.wAttributes & 0xFF)));

        for (size_t i = 0; i < _countof(info.ColorTable); i++)
        {
            dict.Set(std::to_wstring(i), Json::Value(std::to_wstring(info.ColorTable[i])));
        }
    }

    return Json::Value(std::move(dict));
}

static void HandleSetColors(const Json::Value& value)
{
    if (!value.IsDict())
    {
        return;
    }

    HANDLE handle = ::GetStdHandle(STD_OUTPUT_HANDLE);
    CONSOLE_SCREEN_BUFFER_INFOEX info;
    info.cbSize = sizeof(info);

    if (::GetConsoleScreenBufferInfoEx(handle, &info))
    {
        Json::Value indexes = value.GetDict().Get(L"indexes");
        if (indexes.IsString())
        {
            unsigned long value = std::wcstoul(indexes.GetString().c_str(), nullptr, 10);
            if (value)
            {
                info.wAttributes = (info.wAttributes & 0xFF00) | static_cast<WORD>(value & 0xFF);
            }
        }

        for (size_t i = 0; i < _countof(info.ColorTable); i++)
        {
            Json::Value color = value.GetDict().Get(std::to_wstring(i));
            if (color.IsString())
            {
                unsigned long value = std::wcstoul(color.GetString().c_str(), nullptr, 10);
                if (value)
                {
                    info.ColorTable[i] = value;
                }
            }
        }

        ::SetConsoleScreenBufferInfoEx(handle, &info);
    }
}

static Json::Dict HandleGetState(const Json::Dict& input)
{
    Json::Dict dict;

    dict.Set(PIPE_PROPERTY_ALIASES, ::HandleGetAliases());
    dict.Set(PIPE_PROPERTY_COLORS, ::HandleGetColors());
    dict.Set(PIPE_PROPERTY_DIRECTORY, ::HandleGetDirectory());
    dict.Set(PIPE_PROPERTY_ENVIRONMENT, ::HandleGetEnvironment());
    dict.Set(PIPE_PROPERTY_EXECUTABLE, ::HandleGetExecutable());
    dict.Set(PIPE_PROPERTY_TITLE, ::HandleGetTitle());

    return dict;
}

static Json::Dict HandleSetState(const Json::Dict& input)
{
    Json::Value aliasesValue = input.Get(PIPE_PROPERTY_ALIASES);
    Json::Value colorsValue = input.Get(PIPE_PROPERTY_COLORS);
    Json::Value directoryValue = input.Get(PIPE_PROPERTY_DIRECTORY);
    Json::Value environmentValue = input.Get(PIPE_PROPERTY_ENVIRONMENT);
    Json::Value titleValue = input.Get(PIPE_PROPERTY_TITLE);

    ::HandleSetAliases(aliasesValue);
    ::HandleSetColors(colorsValue);
    //::HandleSetDirectory(directoryValue);
    //::HandleSetEnvironment(environmentValue);
    ::HandleSetTitle(titleValue);

    return Json::Dict();
}

static Json::Dict HandleCheckWindowSize(const Json::Dict& input)
{
    DevInject::CheckConsoleWindowSize(false);
    return Json::Dict();
}

static Json::Dict HandleCheckWindowDpi(const Json::Dict& input)
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

    return Json::Dict();
}

static Json::Dict HandleActivated(const Json::Dict& input)
{
    DevInject::CheckConsoleWindowSize(false);
    return Json::Dict();
}

static Json::Dict HandleDetach(const Json::Dict& input)
{
    DevInject::BeginDetach();
    return Json::Dict();
}

// Handles commands comming in from the owner app
Json::MessageHandler DevInject::CreateMessageHandler()
{
    Json::MessageHandlers handlers;
    handlers.emplace(PIPE_COMMAND_GET_STATE, ::HandleGetState);
    handlers.emplace(PIPE_COMMAND_SET_STATE, ::HandleSetState);
    handlers.emplace(PIPE_COMMAND_CHECK_WINDOW_SIZE, ::HandleCheckWindowSize);
    handlers.emplace(PIPE_COMMAND_CHECK_WINDOW_DPI, ::HandleCheckWindowDpi);
    handlers.emplace(PIPE_COMMAND_ACTIVATED, ::HandleActivated);
    handlers.emplace(PIPE_COMMAND_DETACH, ::HandleDetach);

    return [handlers](const Json::Dict& dict)
    {
        return Json::CallMessageHandler(handlers, dict);
    };
}
