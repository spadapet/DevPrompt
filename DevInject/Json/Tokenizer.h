#pragma once

#include "Json/Value.h"

namespace Json
{
    enum class TokenType
    {
        None,
        Error,
        True,
        False,
        Null,
        String,
        Number,
        Comma,
        Colon,
        OpenCurly,
        CloseCurly,
        OpenBracket,
        CloseBracket,
    };

    struct Token
    {
        Value GetValue() const;

        TokenType type;
        const wchar_t* start;
        size_t length;
    };

    class Tokenizer
    {
    public:
        Tokenizer(const wchar_t* text, size_t len = 0);

        Token NextToken();

    private:
        bool SkipString(wchar_t& ch);
        bool SkipNumber(wchar_t& ch);
        bool SkipDigits(wchar_t& ch);
        bool SkipIdentifier(wchar_t& ch);
        wchar_t SkipSpacesAndComments(wchar_t ch);
        wchar_t CurrentChar() const;
        wchar_t NextChar();
        wchar_t PeekNextChar();

        const wchar_t* text;
        const wchar_t* pos;
        const wchar_t* end;
    };
}
