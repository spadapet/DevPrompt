#include "stdafx.h"
#include "Json/Dict.h"
#include "Json/Persist.h"
#include "Json/Tokenizer.h"

static const size_t INDENT_SPACES = 2;

namespace Json
{
    static void WriteValue(const Value& value, size_t spaces, std::wstringstream& output);
    static void WriteObject(const Dict& dict, size_t spaces, std::wstringstream& output);
    static void WriteArray(const std::vector<Value>& values, size_t spaces, std::wstringstream& output);
    static void Encode(const std::wstring& value, std::wstringstream& output);

    static Value ParseValue(Tokenizer& tokenizer, Token* firstToken, const wchar_t** errorPos);
    static Dict ParseObject(Tokenizer& tokenizer, const wchar_t** errorPos);
    static std::vector<Value> ParseArray(Tokenizer& tokenizer, const wchar_t** errorPos);
    static Dict ParseRootObject(Tokenizer& tokenizer, const wchar_t** errorPos);
}

void Json::WriteValue(const Value& value, size_t spaces, std::wstringstream& output)
{
    if (value.IsBool())
    {
        output << value.GetBool() ? L"true" : L"false";
    }
    else if (value.IsInt())
    {
        output << value.GetInt();
    }
    else if (value.IsNumber())
    {
        output << value.GetDouble();
    }
    else if (value.IsString())
    {
        Json::Encode(value.GetString(), output);
    }
    else if (value.IsVector())
    {
        Json::WriteArray(value.GetVector(), spaces, output);
    }
    else if (value.IsDict())
    {
        Json::WriteObject(value.GetDict(), spaces, output);
    }
    else
    {
        output << L"null";
    }
}

void Json::WriteObject(const Dict& dict, size_t spaces, std::wstringstream& output)
{
    output << L'{';

    bool first = true;
    for (auto i : dict)
    {
        if (first)
        {
            first = false;
        }
        else
        {
            output << L',';
        }

        Json::Encode(i.first, output);
        output << L':';
        Json::WriteValue(i.second, spaces + ::INDENT_SPACES, output);
    }

    output << L'}';
}

void Json::WriteArray(const std::vector<Value>& values, size_t spaces, std::wstringstream& output)
{
    output << '[';

    for (size_t i = 0; i < values.size(); i++)
    {
        Json::WriteValue(values[i], spaces + ::INDENT_SPACES, output);

        if (i + 1 < values.size())
        {
            output << ',';
        }
    }

    output << ']';
}

void Json::Encode(const std::wstring& value, std::wstringstream& output)
{
    output << '\"';

    for (const wchar_t* ch = value.c_str(); *ch; ch++)
    {
        switch (*ch)
        {
        case '\"':
            output << L"\\\"";
            break;

        case '\\':
            output << L"\\\\";
            break;

        case '\b':
            output << L"\\b";
            break;

        case '\f':
            output << L"\\f";
            break;

        case '\n':
            output << L"\\n";
            break;

        case '\r':
            output << L"\\r";
            break;

        case '\t':
            output << L"\\t";
            break;

        default:
            if (*ch >= ' ')
            {
                output << *ch;
            }
            break;
        }
    }

    output << L'\"';
}

Json::Value Json::ParseValue(Tokenizer& tokenizer, Token* firstToken, const wchar_t** errorPos)
{
    Token token = firstToken ? *firstToken : tokenizer.NextToken();
    Value value = token.GetValue();

    if (value.IsUnset())
    {
        if (token.type == TokenType::OpenCurly)
        {
            // Nested object
            Json::Dict valueDict = Json::ParseObject(tokenizer, errorPos);
            if (!*errorPos)
            {
                value = Value(std::move(valueDict));
            }
        }
        else if (token.type == TokenType::OpenBracket)
        {
            // Array
            std::vector<Value> valueVector = Json::ParseArray(tokenizer, errorPos);
            if (!*errorPos)
            {
                value = Value(std::move(valueVector));
            }
        }
    }

    if (!*errorPos && value.IsUnset())
    {
        *errorPos = token.start;
    }

    return value;
}

Json::Dict Json::ParseObject(Tokenizer& tokenizer, const wchar_t** errorPos)
{
    Dict dict;

    for (Token token = tokenizer.NextToken(); token.type != TokenType::CloseCurly; )
    {
        // Pair name is first
        if (token.type != TokenType::String)
        {
            *errorPos = token.start;
            break;
        }

        // Get string from token
        Value key = token.GetValue();
        if (key.IsUnset())
        {
            *errorPos = token.start;
            break;
        }

        // Colon must be after name
        token = tokenizer.NextToken();
        if (token.type != TokenType::Colon)
        {
            *errorPos = token.start;
            break;
        }

        Value value = Json::ParseValue(tokenizer, nullptr, errorPos);
        if (*errorPos)
        {
            break;
        }

        dict.Set(std::wstring(key.GetString()), std::move(value));

        token = tokenizer.NextToken();
        if (token.type != TokenType::Comma && token.type != TokenType::CloseCurly)
        {
            *errorPos = token.start;
            break;
        }

        if (token.type == TokenType::Comma)
        {
            token = tokenizer.NextToken();
        }
    }

    return dict;
}

std::vector<Json::Value> Json::ParseArray(Tokenizer& tokenizer, const wchar_t** errorPos)
{
    std::vector<Value> values;

    for (Token token = tokenizer.NextToken(); token.type != TokenType::CloseBracket; )
    {
        Value value = Json::ParseValue(tokenizer, &token, errorPos);
        if (*errorPos)
        {
            break;
        }

        values.push_back(std::move(value));

        token = tokenizer.NextToken();
        if (token.type != TokenType::Comma && token.type != TokenType::CloseBracket)
        {
            *errorPos = token.start;
            break;
        }

        if (token.type == TokenType::Comma)
        {
            token = tokenizer.NextToken();
        }
    }

    return values;
}

Json::Dict Json::ParseRootObject(Tokenizer& tokenizer, const wchar_t** errorPos)
{
    Token token = tokenizer.NextToken();
    if (token.type == TokenType::OpenCurly)
    {
        return Json::ParseObject(tokenizer, errorPos);
    }

    *errorPos = token.start;
    return Dict();
}

Json::Dict Json::Parse(const wchar_t* text, size_t len, size_t* errorPos)
{
    Tokenizer tokenizer(text, len);

    const wchar_t* myErrorPos = nullptr;
    Dict dict = Json::ParseRootObject(tokenizer, &myErrorPos);

    if (errorPos)
    {
        *errorPos = myErrorPos ? (myErrorPos - text) : std::wstring::npos;
    }

    return dict;
}

std::wstring Json::Write(const Dict& dict)
{
    std::wstringstream output;
    Json::WriteObject(dict, 0, output);
    return output.str();
}

// foo=bar\0bar=foo\0\0
Json::Dict Json::ParseNameValuePairs(const wchar_t* text, wchar_t separator)
{
    Dict output;
    std::wstring line;

    while (text && *text)
    {
        size_t lineLen = 0;
        for (const wchar_t* end = text; *end && *end != separator; end++)
        {
            lineLen++;
        }

        line.assign(text, lineLen);
        text += lineLen;

        // Skip the separator (don't skip a null unless the separator is null)
        if (*text || !separator)
        {
            text++;
        }

        size_t equals = line.find('=');
        if (equals != std::wstring::npos)
        {
            std::wstring name = line.substr(0, equals);
            std::wstring value = line.substr(equals + 1);

            if (name.size() && value.size())
            {
                output.Set(std::move(name), Value(std::move(value)));
            }
        }
    }

    return output;
}

std::wstring Json::WriteNameValuePairs(const Dict& dict, wchar_t separator)
{
    std::wstringstream str;

    for (const auto& i : dict)
    {
        if (i.second.IsString())
        {
            str << i.first << L"=" << i.second.GetString() << separator;
        }
    }

    return str.str();
}
