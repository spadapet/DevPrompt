#pragma once

#include "Json/Dict.h"

namespace Json
{
    DEV_INJECT_API Dict Parse(const wchar_t* text, size_t len = 0, size_t * errorPos = nullptr);
    DEV_INJECT_API std::wstring Write(const Dict& dict);

    DEV_INJECT_API Dict ParseNameValuePairs(const wchar_t* text, wchar_t separator);
    DEV_INJECT_API std::wstring WriteNameValuePairs(const Dict& dict, wchar_t separator);
}
