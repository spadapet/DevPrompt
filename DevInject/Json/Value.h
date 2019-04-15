#pragma once

#include "Api.h"

namespace Json
{
    class Dict;

    // One value from a JSON string
    class Value
    {
    public:
        DEV_INJECT_API Value();
        DEV_INJECT_API explicit Value(nullptr_t);
        DEV_INJECT_API explicit Value(bool value);
        DEV_INJECT_API explicit Value(int value);
        DEV_INJECT_API explicit Value(double value);
        DEV_INJECT_API explicit Value(const wchar_t* value);
        DEV_INJECT_API explicit Value(std::wstring&& value);
        DEV_INJECT_API explicit Value(std::vector<Value>&& value);
        DEV_INJECT_API explicit Value(Dict&& value);
        DEV_INJECT_API Value(Value&& rhs);
        DEV_INJECT_API Value(const Value& rhs);
        DEV_INJECT_API ~Value();

        DEV_INJECT_API const Value& operator=(Value&& rhs);
        DEV_INJECT_API const Value& operator=(const Value& rhs);
        DEV_INJECT_API bool operator==(const Value& rhs) const;

        DEV_INJECT_API bool IsUnset() const;
        DEV_INJECT_API bool IsNull() const;
        DEV_INJECT_API bool IsBool() const;
        DEV_INJECT_API bool IsInt() const;
        DEV_INJECT_API bool IsNumber() const;
        DEV_INJECT_API bool IsString() const;
        DEV_INJECT_API bool IsVector() const;
        DEV_INJECT_API bool IsDict() const;

        DEV_INJECT_API bool GetBool() const;
        DEV_INJECT_API int GetInt() const;
        DEV_INJECT_API double GetDouble() const;
        DEV_INJECT_API const std::wstring& GetString() const;
        DEV_INJECT_API std::wstring TryGetString() const;
        DEV_INJECT_API const std::vector<Json::Value> &GetVector() const;
        DEV_INJECT_API const Dict& GetDict() const;

    private:
        void Clear();
        void Move(Value&& rhs);
        void Copy(const Value& rhs);

        enum class Type
        {
            Unset,
            Null,
            Bool,
            Int,
            Double,
            String,
            Vector,
            Dict,
        } type;

        union
        {
            bool boolData;
            int intData;
            double doubleData;
            std::shared_ptr<std::wstring> stringData;
            std::shared_ptr<std::vector<Value>> vectorData;
            std::shared_ptr<Dict> dictData;
        };
    };
}
