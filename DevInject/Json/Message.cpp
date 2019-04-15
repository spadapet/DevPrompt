#include "stdafx.h"
#include "Json/Message.h"

Json::Dict Json::CreateMessage(std::wstring&& commandName)
{
    Dict dict;
    dict.Set(PIPE_PROPERTY_COMMAND, Value(std::move(commandName)));
    return dict;
}

Json::Dict Json::CallMessageHandler(const MessageHandlers& handlers, const Dict& dict)
{
    Value command = dict.Get(PIPE_PROPERTY_COMMAND);
    if (command.IsString())
    {
        auto i = handlers.find(command.GetString());
        if (i != handlers.end())
        {
            return i->second(dict);
        }
    }

    return Dict();
}
