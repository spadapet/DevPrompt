#pragma once

#include "Api.h"

#define PIPE_COMMAND_ACTIVATED L"Activated"
#define PIPE_COMMAND_CLOSED L"Closed"
#define PIPE_COMMAND_CHECK_WINDOW_DPI L"CheckWindowDpi"
#define PIPE_COMMAND_CHECK_WINDOW_SIZE L"CheckWindowSize"
#define PIPE_COMMAND_DEACTIVATED L"Deactivated"
#define PIPE_COMMAND_DETACH L"Detach"
#define PIPE_COMMAND_ENV_CHANGED L"EnvChanged"
#define PIPE_COMMAND_GET_ALIASES L"GetAliases"
#define PIPE_COMMAND_GET_COLOR_TABLE L"GetColorTable"
#define PIPE_COMMAND_GET_CURRENT_DIRECTORY L"GetCurrentDirectory"
#define PIPE_COMMAND_GET_ENV L"GetEnv"
#define PIPE_COMMAND_GET_EXE L"GetExe"
#define PIPE_COMMAND_GET_TITLE L"GetTitle"
#define PIPE_COMMAND_PIPE_CREATED L"PipeCreated"
#define PIPE_COMMAND_SET_ALIASES L"SetAliases"
#define PIPE_COMMAND_SET_COLOR_TABLE L"SetColorTable"
#define PIPE_COMMAND_SET_ENV L"SetEnv"
#define PIPE_COMMAND_SET_TITLE L"SetTitle"
#define PIPE_COMMAND_TITLE_CHANGED L"TitleChanged"
#define PIPE_COMMAND_WINDOW_CREATED L"WindowCreated"

#define PIPE_PROPERTY_VALUE L"Value"

// Key/Value strings that get passed through cross-process pipes
class Message
{
public:
    DEV_INJECT_API Message();
    DEV_INJECT_API Message(const Message& rhs);
    DEV_INJECT_API Message(Message&& rhs);
    DEV_INJECT_API Message(const std::wstring& commandName, unsigned int id = 0, bool isResponse = false);

    DEV_INJECT_API Message& operator=(const Message& rhs);
    DEV_INJECT_API Message& operator=(Message&& rhs);

    DEV_INJECT_API void SetCommand(const std::wstring& name);
    DEV_INJECT_API void SetResponse(const std::wstring& name);
    DEV_INJECT_API void SetId(unsigned int id);
    DEV_INJECT_API void SetValue(const std::wstring& name, const std::wstring& value);

    DEV_INJECT_API const std::wstring& GetCommand() const;
    DEV_INJECT_API const std::wstring& GetResponse() const;
    DEV_INJECT_API unsigned int GetId() const;
    DEV_INJECT_API const std::wstring& GetValue(const std::wstring& name) const;

    DEV_INJECT_API std::vector<std::wstring> GetNames() const;
    DEV_INJECT_API bool HasAnyName() const;
    DEV_INJECT_API std::wstring GetNamesAndValues() const;
    DEV_INJECT_API void ParseNameValuePairs(const wchar_t* cur, wchar_t separator, std::function<std::wstring(const std::wstring&)> nameFilter = nullptr);

    static Message Parse(const BYTE* data, size_t size);
    std::vector<BYTE> Convert() const;
    Message CreateResponse() const;

private:
    std::unordered_map<std::wstring, std::wstring> properties;
};

typedef std::function<Message(const Message& input)> MessageHandler;
