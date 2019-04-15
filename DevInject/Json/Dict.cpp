#include "stdafx.h"
#include "Json/Dict.h"
#include "Json/Persist.h"

Json::Dict::Dict()
{
}

Json::Dict::Dict(Dict&& rhs)
    : values(std::move(rhs.values))
{
}

Json::Dict::Dict(const Dict& rhs)
    : values(rhs.values)
{
}

const Json::Dict& Json::Dict::operator=(Dict&& rhs)
{
    this->values = std::move(rhs.values);
    return *this;
}

const Json::Dict& Json::Dict::operator=(const Dict& rhs)
{
    this->values = rhs.values;
    return *this;
}

bool Json::Dict::operator==(const Dict& rhs) const
{
    return this->values == rhs.values;
}

size_t Json::Dict::Size() const
{
    return this->values.size();
}

void Json::Dict::Set(std::wstring&& key, Value&& value)
{
    if (value.IsUnset())
    {
        this->values.erase(key);
        return;
    }

    auto i = this->values.find(key);
    if (i == this->values.end())
    {
        this->values.emplace(std::move(key), std::move(value));
    }
    else
    {
        i->second = std::move(value);
    }
}

Json::Value Json::Dict::Get(const std::wstring& key) const
{
    Value value = this->GetFromPath(key);
    if (value.IsUnset())
    {
        auto i = this->values.find(key);
        if (i != this->values.end())
        {
            value = i->second;
        }
    }

    return value;
}

Json::Value Json::Dict::GetFromPath(const std::wstring& path) const
{
    if (path.empty() || path[0] != L'/')
    {
        return Value();
    }

    size_t nextThing = path.find_first_of(L"/[", 1, 2);
    std::wstring name = path.substr(1, nextThing - ((nextThing != std::wstring::npos) ? 1 : 0));
    Value value = this->Get(name);

    for (; !value.IsUnset() && nextThing != std::wstring::npos; nextThing = path.find_first_of(L"/[", nextThing + 1, 2))
    {
        if (path[nextThing] == '/')
        {
            if (!value.IsDict())
            {
                return Value();
            }

            value = value.GetDict().Get(path.substr(nextThing));
            break;
        }
        else // '['
        {
            if (!value.IsVector())
            {
                return Value();
            }

            wchar_t* parseEnd = nullptr;
            size_t index = static_cast<size_t>(wcstoul(path.c_str() + nextThing + 1, &parseEnd, 10));
            if (*parseEnd != ']' || index >= value.GetVector().size())
            {
                return Value();
            }

            value = value.GetVector()[index];
        }
    }

    return value;
}

Json::Dict::MapType::const_iterator Json::Dict::begin() const
{
    return this->values.begin();
}

Json::Dict::MapType::const_iterator Json::Dict::end() const
{
    return this->values.end();
}

void Json::Dict::DebugDump() const
{
    std::wstring str = Json::Write(*this);
    str += L"\r\n";

    ::OutputDebugString(str.c_str());
}
