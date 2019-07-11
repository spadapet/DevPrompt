namespace DevPrompt.Utility.Json
{
    internal class JsonTokenizer
    {
        private string json;
        private int pos;

        public JsonTokenizer(string json)
        {
            this.json = json ?? string.Empty;
        }

        public JsonToken NextToken
        {
            get
            {
                char ch = this.SkipSpacesAndComments(this.CurrentChar);
                JsonTokenType type = JsonTokenType.Error;
                int start = this.pos;

                switch (ch)
                {
                    case '\0':
                        type = JsonTokenType.None;
                        break;

                    case 't':
                        if (this.SkipIdentifier(ref ch) &&
                            this.pos - start == 4 &&
                            this.json[start + 1] == 'r' &&
                            this.json[start + 2] == 'u' &&
                            this.json[start + 3] == 'e')
                        {
                            type = JsonTokenType.True;
                        }
                        break;

                    case 'f':
                        if (this.SkipIdentifier(ref ch) &&
                            this.pos - start == 5 &&
                            this.json[start + 1] == 'a' &&
                            this.json[start + 2] == 'l' &&
                            this.json[start + 3] == 's' &&
                            this.json[start + 4] == 'e')
                        {
                            type = JsonTokenType.False;
                        }
                        break;

                    case 'n':
                        if (this.SkipIdentifier(ref ch) &&
                            this.pos - start == 4 &&
                            this.json[start + 1] == 'u' &&
                            this.json[start + 2] == 'l' &&
                            this.json[start + 3] == 'l')
                        {
                            type = JsonTokenType.Null;
                        }
                        break;

                    case '\"':
                        if (this.SkipString(ref ch))
                        {
                            type = JsonTokenType.String;
                        }
                        break;

                    case ',':
                        this.pos++;
                        type = JsonTokenType.Comma;
                        break;

                    case ':':
                        this.pos++;
                        type = JsonTokenType.Colon;
                        break;

                    case '{':
                        this.pos++;
                        type = JsonTokenType.OpenCurly;
                        break;

                    case '}':
                        this.pos++;
                        type = JsonTokenType.CloseCurly;
                        break;

                    case '[':
                        this.pos++;
                        type = JsonTokenType.OpenBracket;
                        break;

                    case ']':
                        this.pos++;
                        type = JsonTokenType.CloseBracket;
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
                        if (this.SkipNumber(ref ch))
                        {
                            type = JsonTokenType.Number;
                        }
                        break;
                }

                return new JsonToken(type, start, this.pos - start);
            }
        }

        private static bool IsHexDigit(char ch)
        {
            if (char.IsDigit(ch))
            {
                return true;
            }

            if (char.IsLetter(ch))
            {
                switch (char.ToLowerInvariant(ch))
                {
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                        return true;
                }
            }

            return false;
        }

        private bool SkipString(ref char ch)
        {
            if (ch != '\"')
            {
                return false;
            }

            ch = this.NextChar;

            while (true)
            {
                if (ch == '\"')
                {
                    this.pos++;
                    break;
                }
                else if (ch == '\\')
                {
                    ch = this.NextChar;

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
                            ch = this.NextChar;
                            break;

                        case 'u':
                            if (this.pos > this.json.Length - 5)
                            {
                                return false;
                            }

                            if (!JsonTokenizer.IsHexDigit(this.json[this.pos + 1]) ||
                                !JsonTokenizer.IsHexDigit(this.json[this.pos + 2]) ||
                                !JsonTokenizer.IsHexDigit(this.json[this.pos + 3]) ||
                                !JsonTokenizer.IsHexDigit(this.json[this.pos + 4]))
                            {
                                return false;
                            }

                            this.pos += 5;
                            ch = this.CurrentChar;
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
                    ch = this.NextChar;
                }
            }

            return true;
        }

        private bool SkipNumber(ref char ch)
        {
            if (ch == '-')
            {
                ch = this.NextChar;
            }

            if (!this.SkipDigits(ref ch))
            {
                return false;
            }

            if (ch == '.')
            {
                ch = this.NextChar;

                if (!this.SkipDigits(ref ch))
                {
                    return false;
                }
            }

            if (ch == 'e' || ch == 'E')
            {
                ch = this.NextChar;

                if (ch == '-' || ch == '+')
                {
                    ch = this.NextChar;
                }

                if (!this.SkipDigits(ref ch))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SkipDigits(ref char ch)
        {
            if (!char.IsDigit(ch))
            {
                return false;
            }

            do
            {
                ch = this.NextChar;
            } while (char.IsDigit(ch));

            return true;
        }

        private bool SkipIdentifier(ref char ch)
        {
            if (!char.IsLetter(ch))
            {
                return false;
            }

            do
            {
                ch = this.NextChar;
            } while (char.IsLetterOrDigit(ch));

            return true;
        }

        private char SkipSpacesAndComments(char ch)
        {
            while (true)
            {
                if (char.IsWhiteSpace(ch))
                {
                    ch = this.NextChar;
                }
                else if (ch == '/')
                {
                    char ch2 = this.PeekNextChar;

                    if (ch2 == '/')
                    {
                        this.pos++;
                        ch = this.NextChar;

                        while (ch != '\0' && ch != '\r' && ch != '\n')
                        {
                            ch = this.NextChar;
                        }
                    }
                    else if (ch2 == '*')
                    {
                        int start = this.pos++;
                        ch = this.NextChar;

                        while (ch != '\0' && (ch != '*' || this.PeekNextChar != '/'))
                        {
                            ch = this.NextChar;
                        }

                        if (ch == '\0')
                        {
                            // No end for the comment
                            this.pos = start;
                            ch = '/';
                            break;
                        }

                        // Skip the end of comment
                        this.pos++;
                        ch = this.NextChar;
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

        private char CurrentChar => this.pos < this.json.Length ? this.json[this.pos] : '\0';
        private char NextChar => ++this.pos < this.json.Length ? this.json[this.pos] : '\0';
        private char PeekNextChar => this.pos < this.json.Length - 1 ? this.json[this.pos + 1] : '\0';
    }
}
