#include "stdafx.h"
#include "Message.h"

static std::wstring EMPTY_STRING;
static std::wstring PIPE_MAP_TYPE_COMMAND = L"[!!Command!!]";
static std::wstring PIPE_MAP_TYPE_RESPONSE = L"[!!Response!!]";
static std::wstring PIPE_MAP_TYPE_ID = L"[!!ID!!]";

static void AppendString(std::vector<BYTE>& data, const std::wstring& str)
{
    size_t oldDataSize = data.size();
    data.resize(data.size() + sizeof(DWORD) + str.size() * sizeof(wchar_t));

    BYTE* writeData = data.data() + oldDataSize;
    DWORD strLen = static_cast<DWORD>(str.size());

    std::memcpy(writeData, &strLen, sizeof(DWORD));
    std::memcpy(writeData + sizeof(DWORD), str.c_str(), str.size() * sizeof(wchar_t));
}

static std::wstring GetString(const BYTE*& data, size_t& size)
{
    if (size >= sizeof(DWORD))
    {
        DWORD strLen;
        std::memcpy(&strLen, data, sizeof(DWORD));

        data += sizeof(DWORD);
        size -= sizeof(DWORD);

        if (size >= strLen * sizeof(wchar_t))
        {
            std::wstring str;
            str.resize(strLen);

            std::memcpy(str.data(), data, strLen * sizeof(wchar_t));

            data += strLen * sizeof(wchar_t);
            size -= strLen * sizeof(wchar_t);

            return str;
        }
    }

    assert(false);
    data += size;
    size = 0;

    return std::wstring();
}

Message::Message()
{
}

Message::Message(const Message& rhs)
    : properties(rhs.properties)
{
}

Message::Message(Message&& rhs)
    : properties(std::move(rhs.properties))
{
}

Message::Message(const std::wstring& commandName, unsigned int id, bool isResponse)
{
    if (isResponse)
    {
        this->SetResponse(commandName);
    }
    else
    {
        this->SetCommand(commandName);
    }

    if (id)
    {
        this->SetId(id);
    }
}

Message& Message::operator=(const Message& rhs)
{
    if (this != &rhs)
    {
        this->properties = rhs.properties;
    }

    return *this;
}

Message& Message::operator=(Message&& rhs)
{
    if (this != &rhs)
    {
        this->properties = std::move(rhs.properties);
    }

    return *this;
}

Message Message::Parse(const BYTE* data, size_t size)
{
    Message output;

    while (size)
    {
        std::wstring key = ::GetString(data, size);
        std::wstring value = ::GetString(data, size);

        if (key.size() && value.size())
        {
            output.SetValue(key, value);
        }
    }

    return output;
}

std::vector<BYTE> Message::Convert() const
{
    size_t reserve = this->properties.size() * 2 * sizeof(DWORD);

    for (const auto& i : this->properties)
    {
        reserve += i.first.size() * sizeof(wchar_t);
        reserve += i.second.size() * sizeof(wchar_t);
    }

    std::vector<BYTE> bytes;
    bytes.reserve(reserve);

    for (const auto& i : this->properties)
    {
        ::AppendString(bytes, i.first);
        ::AppendString(bytes, i.second);
    }

    return bytes;
}

Message Message::CreateResponse() const
{
    return Message(GetCommand(), GetId(), true);
}

// foo=bar\0bar=foo\0\0
void Message::ParseNameValuePairs(const wchar_t* cur, wchar_t separator, std::function<std::wstring(const std::wstring&)> nameFilter)
{
    std::wstring line;

    while (cur && *cur)
    {
        size_t lineLen = 0;
        for (const wchar_t* end = cur; *end && *end != separator; end++)
        {
            lineLen++;
        }

        line.assign(cur, lineLen);
        cur += lineLen;

        // Skip the separator (don't skip a null unless the separator is null)
        if (*cur || !separator)
        {
            cur++;
        }

        size_t equals = line.find('=');
        if (equals != std::wstring::npos)
        {
            std::wstring name = line.substr(0, equals);
            std::wstring value = line.substr(equals + 1);

            if (name.size() && nameFilter != nullptr)
            {
                name = nameFilter(name);
            }

            if (name.size() && value.size())
            {
                this->SetValue(name, value);
            }
        }
    }
}

void Message::SetCommand(const std::wstring& name)
{
    this->SetValue(PIPE_MAP_TYPE_COMMAND, name);
    this->properties.erase(PIPE_MAP_TYPE_RESPONSE);
}

void Message::SetResponse(const std::wstring& name)
{
    this->SetValue(PIPE_MAP_TYPE_RESPONSE, name);
    this->properties.erase(PIPE_MAP_TYPE_COMMAND);
}

void Message::SetId(unsigned int id)
{
    assert(id);
    this->SetValue(PIPE_MAP_TYPE_ID, std::to_wstring(id));
}

void Message::SetValue(const std::wstring& name, const std::wstring& value)
{
    this->properties[name] = value;
}

const std::wstring& Message::GetCommand() const
{
    return this->GetValue(PIPE_MAP_TYPE_COMMAND);
}

const std::wstring& Message::GetResponse() const
{
    return this->GetValue(PIPE_MAP_TYPE_RESPONSE);
}

unsigned int Message::GetId() const
{
    std::wstring value = this->GetValue(PIPE_MAP_TYPE_ID);
    if (value.size())
    {
        const wchar_t* start = value.c_str();
        wchar_t* end = nullptr;
        unsigned int id = std::wcstoul(start, &end, 10);

        if (id && end == start + value.size())
        {
            return id;
        }

        assert(false);
    }

    return 0;
}

const std::wstring& Message::GetValue(const std::wstring& name) const
{
    auto i = this->properties.find(name);
    return (i != this->properties.end()) ? i->second : ::EMPTY_STRING;
}

std::vector<std::wstring> Message::GetNames() const
{
    std::vector<std::wstring> names;
    names.reserve(this->properties.size());

    for (auto i = this->properties.begin(); i != this->properties.end(); i++)
    {
        const std::wstring& name = i->first;
        if (std::wcsncmp(name.c_str(), L"[!!", 3))
        {
            names.push_back(name);
        }
    }

    return names;
}

bool Message::HasAnyName() const
{
    for (auto i = this->properties.begin(); i != this->properties.end(); i++)
    {
        const std::wstring& name = i->first;
        if (std::wcsncmp(name.c_str(), L"[!!", 3))
        {
            return true;
        }
    }

    return false;
}

std::wstring Message::GetNamesAndValues() const
{
    std::vector<std::wstring> names = this->GetNames();
    size_t size = 0;

    for (const std::wstring& name : names)
    {
        const std::wstring& value = this->GetValue(name);
        size += name.size() + value.size() + 2;
    }

    std::wstring output;
    output.reserve(size * sizeof(wchar_t));

    for (const std::wstring& name : names)
    {
        const std::wstring& value = this->GetValue(name);

        if (name.find_first_of(L"=\n\0") == std::wstring::npos &&
            value.find_first_of(L"\n\0") == std::wstring::npos)
        {
            output += name;
            output += '=';
            output += value;
            output += '\n';
        }
    }

    return output;
}
