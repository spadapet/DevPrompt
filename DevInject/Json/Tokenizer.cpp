#include "stdafx.h"
#include "Json/Tokenizer.h"

Json::Value Json::Token::GetValue() const
{
    switch (this->type)
    {
    case TokenType::True:
        return Value(true);

    case TokenType::False:
        return Value(false);

    case TokenType::Null:
        return Value(nullptr);

    case TokenType::Number:
    {
        wchar_t* end = nullptr;
        double val = wcstod(this->start, &end);

        if (end == this->start + this->length)
        {
            if (std::floor(val) == val && val >= static_cast<double>(INT_MIN) && val <= static_cast<double>(INT_MAX))
            {
                return Value(static_cast<int>(val));
            }
            else
            {
                return Value(val);
            }
        }
    }
    break;

    case TokenType::String:
    {
        std::wstring val;
        val.reserve(this->length);

        const wchar_t* cur = this->start + 1;
        for (const wchar_t* end = this->start + this->length - 1; cur && cur < end; )
        {
            if (*cur == '\\')
            {
                switch (cur[1])
                {
                case '\"':
                case '\\':
                case '/':
                    val.append(1, cur[1]);
                    cur += 2;
                    break;

                case 'b':
                    val.append(1, '\b');
                    cur += 2;
                    break;

                case 'f':
                    val.append(1, '\f');
                    cur += 2;
                    break;

                case 'n':
                    val.append(1, '\n');
                    cur += 2;
                    break;

                case 'r':
                    val.append(1, '\r');
                    cur += 2;
                    break;

                case 't':
                    val.append(1, '\t');
                    cur += 2;
                    break;

                case 'u':
                    if (cur + 5 < end)
                    {
                        wchar_t buffer[5] = { cur[2], cur[3], cur[4], cur[5], '\0' };
                        wchar_t* stopped = nullptr;
                        unsigned long decoded = wcstoul(buffer, &stopped, 16);

                        if (!*stopped)
                        {
                            val.append(1, (wchar_t)(decoded & 0xFFFF));
                            cur += 6;
                        }
                        else
                        {
                            cur = nullptr;
                        }
                    }
                    else
                    {
                        cur = nullptr;
                    }
                    break;

                default:
                    cur = nullptr;
                    break;
                }
            }
            else
            {
                val.append(1, *cur);
                cur++;
            }
        }

        if (cur)
        {
            return Value(std::move(val));
        }
    }
    break;
    }

    return Value();
}

Json::Tokenizer::Tokenizer(const wchar_t* text, size_t len)
    : text(text)
    , pos(text)
    , end(text + (len ? len : std::wcslen(text)))
{
}

Json::Token Json::Tokenizer::NextToken()
{
    wchar_t ch = SkipSpacesAndComments(CurrentChar());
    TokenType type = TokenType::Error;
    const wchar_t* start = this->pos;

    switch (ch)
    {
    case '\0':
        type = TokenType::None;
        break;

    case 't':
        if (SkipIdentifier(ch) &&
            this->pos - start == 4 &&
            start[1] == 'r' &&
            start[2] == 'u' &&
            start[3] == 'e')
        {
            type = TokenType::True;
        }
        break;

    case 'f':
        if (SkipIdentifier(ch) &&
            this->pos - start == 5 &&
            start[1] == 'a' &&
            start[2] == 'l' &&
            start[3] == 's' &&
            start[4] == 'e')
        {
            type = TokenType::False;
        }
        break;

    case 'n':
        if (SkipIdentifier(ch) &&
            this->pos - start == 4 &&
            start[1] == 'u' &&
            start[2] == 'l' &&
            start[3] == 'l')
        {
            type = TokenType::Null;
        }
        break;

    case '\"':
        if (SkipString(ch))
        {
            type = TokenType::String;
        }
        break;

    case ',':
        this->pos++;
        type = TokenType::Comma;
        break;

    case ':':
        this->pos++;
        type = TokenType::Colon;
        break;

    case '{':
        this->pos++;
        type = TokenType::OpenCurly;
        break;

    case '}':
        this->pos++;
        type = TokenType::CloseCurly;
        break;

    case '[':
        this->pos++;
        type = TokenType::OpenBracket;
        break;

    case ']':
        this->pos++;
        type = TokenType::CloseBracket;
        break;

    case '-':
    case '0':
    case '1':
    case '2':
    case '3':
    case '4':
    case '5':
    case '6':
    case '7':
    case '8':
    case '9':
        if (SkipNumber(ch))
        {
            type = TokenType::Number;
        }
        break;
    }

    return Token{ type, start, (size_t)(this->pos - start) };
}

bool Json::Tokenizer::SkipString(wchar_t& ch)
{
    if (ch != '\"')
    {
        return false;
    }

    ch = NextChar();

    while (true)
    {
        if (ch == '\"')
        {
            this->pos++;
            break;
        }
        else if (ch == '\\')
        {
            ch = NextChar();

            switch (ch)
            {
            case '\"':
            case '\\':
            case '/':
            case 'b':
            case 'f':
            case 'n':
            case 'r':
            case 't':
                ch = NextChar();
                break;

            case 'u':
                if (this->pos > this->end - 5)
                {
                    return false;
                }

                if (!iswxdigit(this->pos[1]) ||
                    !iswxdigit(this->pos[2]) ||
                    !iswxdigit(this->pos[3]) ||
                    !iswxdigit(this->pos[4]))
                {
                    return false;
                }

                this->pos += 5;
                ch = CurrentChar();
                break;

            default:
                return false;
            }
        }
        else if (ch < ' ')
        {
            return false;
        }
        else
        {
            ch = NextChar();
        }
    }

    return true;
}

bool Json::Tokenizer::SkipNumber(wchar_t& ch)
{
    if (ch == '-')
    {
        ch = NextChar();
    }

    if (!SkipDigits(ch))
    {
        return false;
    }

    if (ch == '.')
    {
        ch = NextChar();

        if (!SkipDigits(ch))
        {
            return false;
        }
    }

    if (ch == 'e' || ch == 'E')
    {
        ch = NextChar();

        if (ch == '-' || ch == '+')
        {
            ch = NextChar();
        }

        if (!SkipDigits(ch))
        {
            return false;
        }
    }

    return true;
}

bool Json::Tokenizer::SkipDigits(wchar_t& ch)
{
    if (!iswdigit(ch))
    {
        return false;
    }

    do
    {
        ch = NextChar();
    } while (iswdigit(ch));

    return true;
}

bool Json::Tokenizer::SkipIdentifier(wchar_t& ch)
{
    if (!iswalpha(ch))
    {
        return false;
    }

    do
    {
        ch = NextChar();
    } while (iswalnum(ch));

    return true;
}

wchar_t Json::Tokenizer::SkipSpacesAndComments(wchar_t ch)
{
    while (true)
    {
        if (iswspace(ch))
        {
            ch = NextChar();
        }
        else if (ch == '/')
        {
            wchar_t ch2 = PeekNextChar();

            if (ch2 == '/')
            {
                this->pos++;
                ch = NextChar();

                while (ch && ch != '\r' && ch != '\n')
                {
                    ch = NextChar();
                }
            }
            else if (ch2 == '*')
            {
                const wchar_t* start = this->pos++;
                ch = NextChar();

                while (ch && (ch != '*' || PeekNextChar() != '/'))
                {
                    ch = NextChar();
                }

                if (!ch)
                {
                    // No end for the comment
                    this->pos = start;
                    ch = '/';
                    break;
                }

                // Skip the end of comment
                this->pos++;
                ch = NextChar();
            }
            else
            {
                break;
            }
        }
        else
        {
            break;
        }
    }

    return ch;
}

wchar_t Json::Tokenizer::CurrentChar() const
{
    return this->pos < this->end ? *this->pos : '\0';
}

wchar_t Json::Tokenizer::NextChar()
{
    return ++this->pos < this->end ? *this->pos : '\0';
}

wchar_t Json::Tokenizer::PeekNextChar()
{
    return this->pos < this->end - 1 ? this->pos[1] : '\0';
}
