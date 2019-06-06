#include "stdafx.h"
#include "Json/Dict.h"

Json::Value::Value()
    : type(Type::Unset)
{
}

Json::Value::Value(nullptr_t)
    : type(Type::Null)
{
}

Json::Value::Value(bool value)
    : type(Type::Bool)
    , boolData(value)
{
}

Json::Value::Value(int value)
    : type(Type::Int)
    , intData(value)
{
}

Json::Value::Value(double value)
    : type(Type::Double)
    , doubleData(value)
{
}

Json::Value::Value(const wchar_t* value)
    : type(Type::String)
    , stringData(std::make_shared<std::wstring>(value))
{
}

Json::Value::Value(std::wstring&& value)
    : type(Type::String)
    , stringData(std::make_shared<std::wstring>(std::move(value)))
{
}

Json::Value::Value(std::vector<Value>&& value)
    : type(Type::Vector)
    , vectorData(std::make_shared<std::vector<Value>>(std::move(value)))
{
}

Json::Value::Value(Dict&& value)
    : type(Type::Dict)
    , dictData(std::make_shared<Dict>(std::move(value)))
{
}

Json::Value::Value(Value&& rhs)
    : type(Type::Unset)
{
    this->Move(std::move(rhs));
}

Json::Value::Value(const Value& rhs)
    : type(Type::Unset)
{
    this->Copy(rhs);
}

Json::Value::~Value()
{
    this->Clear();
}

const Json::Value& Json::Value::operator=(Value&& rhs)
{
    this->Move(std::move(rhs));
    return *this;
}

const Json::Value& Json::Value::operator=(const Value& rhs)
{
    this->Copy(rhs);
    return *this;
}

bool Json::Value::operator==(const Value& rhs) const
{
    if (this->type == rhs.type)
    {
        switch (this->type)
        {
        case Type::Bool:
            return this->boolData == rhs.boolData;

        case Type::Int:
            return this->intData == rhs.intData;

        case Type::Double:
            return this->doubleData == rhs.doubleData;

        case Type::String:
            return *this->stringData == *rhs.stringData;

        case Type::Vector:
            return *this->vectorData == *rhs.vectorData;

        case Type::Dict:
            return *this->dictData == *rhs.dictData;

        default:
            return true;
        }
    }

    return false;
}

bool Json::Value::IsUnset() const
{
    return this->type == Type::Unset;
}

bool Json::Value::IsNull() const
{
    return this->type == Type::Null;
}

bool Json::Value::IsBool() const
{
    return this->type == Type::Bool;
}

bool Json::Value::IsInt() const
{
    return this->type == Type::Int;
}

bool Json::Value::IsNumber() const
{
    return this->type == Type::Int || this->type == Type::Double;
}

bool Json::Value::IsString() const
{
    return this->type == Type::String;
}

bool Json::Value::IsVector() const
{
    return this->type == Type::Null;
}

bool Json::Value::IsDict() const
{
    return this->type == Type::Dict;
}

bool Json::Value::GetBool() const
{
    assert(this->IsBool());
    return this->boolData;
}

int Json::Value::GetInt() const
{
    assert(this->IsInt());
    return this->intData;
}

double Json::Value::GetDouble() const
{
    assert(this->IsNumber());
    return (this->type == Type::Int) ? static_cast<double>(this->intData) : this->doubleData;
}

const std::wstring& Json::Value::GetString() const
{
    assert(this->IsString());
    return *this->stringData;
}

std::wstring Json::Value::TryGetString() const
{
    return this->IsString() ? this->GetString() : std::wstring();
}

HWND Json::Value::TryGetHwndFromString() const
{
    HWND hwnd = nullptr;
    std::wstring hwndString = this->TryGetString();
    const wchar_t* start = hwndString.c_str();
    wchar_t* end = nullptr;
    unsigned long long hwndSize = std::wcstoull(start, &end, 10);

    if (hwndSize && end == start + hwndString.size())
    {
        hwnd = reinterpret_cast<HWND>(hwndSize);
    }

    return hwnd;
}

const std::vector<Json::Value> &Json::Value::GetVector() const
{
    assert(this->IsVector());
    return *this->vectorData;
}

const Json::Dict& Json::Value::GetDict() const
{
    assert(this->IsDict());
    return *this->dictData;
}

void Json::Value::Clear()
{
    switch (this->type)
    {
    case Type::Dict:
        this->dictData.~shared_ptr<Dict>();
        break;

    case Type::String:
        this->stringData.~shared_ptr<std::wstring>();
        break;

    case Type::Vector:
        this->vectorData.~shared_ptr<std::vector<Value>>();
        break;
    }

    this->type = Type::Unset;
}

void Json::Value::Move(Value&& rhs)
{
    if (this != &rhs)
    {
        this->Clear();
        Type type = (this->type = rhs.type);
        rhs.type = Type::Unset;

        switch (type)
        {
        case Type::Bool:
            this->boolData = rhs.boolData;
            break;

        case Type::Int:
            this->intData = rhs.intData;
            break;

        case Type::Double:
            this->doubleData = rhs.doubleData;
            break;

        case Type::String:
            ::new(&this->stringData) std::shared_ptr<std::wstring>(std::move(rhs.stringData));
            break;

        case Type::Vector:
            ::new(&this->vectorData) std::shared_ptr<std::vector<Value>>(std::move(rhs.vectorData));
            break;

        case Type::Dict:
            ::new(&this->dictData) std::shared_ptr<Dict>(std::move(rhs.dictData));
            break;
        }
    }
}

void Json::Value::Copy(const Value& rhs)
{
    if (this != &rhs)
    {
        this->Clear();
        this->type = rhs.type;

        switch (rhs.type)
        {
        case Type::Bool:
            this->boolData = rhs.boolData;
            break;

        case Type::Int:
            this->intData = rhs.intData;
            break;

        case Type::Double:
            this->doubleData = rhs.doubleData;
            break;

        case Type::String:
            ::new(&this->stringData) std::shared_ptr<std::wstring>(rhs.stringData);
            break;

        case Type::Vector:
            ::new(&this->vectorData) std::shared_ptr<std::vector<Value>>(rhs.vectorData);
            break;

        case Type::Dict:
            ::new(&this->dictData) std::shared_ptr<Dict>(rhs.dictData);
            break;
        }
    }
}
