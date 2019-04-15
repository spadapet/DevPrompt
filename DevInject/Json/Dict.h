#pragma once

#include "Json/Value.h"

namespace Json
{
    // A JSON object
    class Dict
    {
    public:
        DEV_INJECT_API Dict();
        DEV_INJECT_API Dict(Dict&& rhs);
        DEV_INJECT_API Dict(const Dict& rhs);

        DEV_INJECT_API const Dict& operator=(Dict&& rhs);
        DEV_INJECT_API const Dict& operator=(const Dict& rhs);
        DEV_INJECT_API bool operator==(const Dict& rhs) const;

        DEV_INJECT_API size_t Size() const;
        DEV_INJECT_API void Set(std::wstring&& key, Value&& value);
        DEV_INJECT_API Value Get(const std::wstring& key) const;

        typedef std::unordered_map<std::wstring, Value> MapType;
        DEV_INJECT_API MapType::const_iterator begin() const;
        DEV_INJECT_API MapType::const_iterator end() const;

        void DebugDump() const;

    private:
        Value GetFromPath(const std::wstring& path) const;

        MapType values;
    };
}
