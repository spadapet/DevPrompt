#pragma once

#include "Json/Dict.h"

#define PIPE_COMMAND_ACTIVATED L"Activated"
#define PIPE_COMMAND_CHECK_WINDOW_DPI L"CheckWindowDpi"
#define PIPE_COMMAND_CHECK_WINDOW_SIZE L"CheckWindowSize"
#define PIPE_COMMAND_CLOSED L"Closed"
#define PIPE_COMMAND_CONHOST_INJECTED L"ConhostInjected"
#define PIPE_COMMAND_DEACTIVATED L"Deactivated"
#define PIPE_COMMAND_DETACH L"Detach"
#define PIPE_COMMAND_GET_STATE L"GetState"
#define PIPE_COMMAND_PIPE_CREATED L"PipeCreated"
#define PIPE_COMMAND_SET_STATE L"SetState"
#define PIPE_COMMAND_STATE_CHANGED L"StateChanged"
#define PIPE_COMMAND_WINDOW_CREATED L"WindowCreated"

#define PIPE_PROPERTY_ALIASES L"Aliases"
#define PIPE_PROPERTY_ARGUMENTS L"Arguments"
#define PIPE_PROPERTY_COLORS L"Colors"
#define PIPE_PROPERTY_COMMAND L"Command"
#define PIPE_PROPERTY_DIRECTORY L"Directory"
#define PIPE_PROPERTY_ENVIRONMENT L"Environment"
#define PIPE_PROPERTY_EXECUTABLE L"Executable"
#define PIPE_PROPERTY_HWND L"HWND"
#define PIPE_PROPERTY_ID L"ID"
#define PIPE_PROPERTY_TITLE L"Title"

namespace Json
{
    typedef std::function<Dict(const Dict& dict)> MessageHandler;
    typedef std::unordered_map<std::wstring, MessageHandler> MessageHandlers;

    DEV_INJECT_API Dict CreateMessage(std::wstring&& commandName);
    DEV_INJECT_API Dict CallMessageHandler(const MessageHandlers& handlers, const Dict& dict);
}
