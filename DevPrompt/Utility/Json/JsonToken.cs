using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace DevPrompt.Utility.Json
{
    internal enum JsonTokenType
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
    }

    [DebuggerDisplay("{Type}")]
    internal struct JsonToken
    {
        public JsonTokenType Type { get; }
        public int Start { get; }
        public int Length { get; }

        public JsonToken(JsonTokenType type, int start, int length)
        {
            this.Type = type;
            this.Start = start;
            this.Length = length;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsNone => this.Type == JsonTokenType.None;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsError => this.Type == JsonTokenType.Error;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsTrue => this.Type == JsonTokenType.True;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsFalse => this.Type == JsonTokenType.False;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsNull => this.Type == JsonTokenType.Null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsString => this.Type == JsonTokenType.String;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsNumber => this.Type == JsonTokenType.Number;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsComma => this.Type == JsonTokenType.Comma;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsColon => this.Type == JsonTokenType.Colon;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsOpenCurly => this.Type == JsonTokenType.OpenCurly;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsCloseCurly => this.Type == JsonTokenType.CloseCurly;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsOpenBracket => this.Type == JsonTokenType.OpenBracket;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsCloseBracket => this.Type == JsonTokenType.CloseBracket;

        public JsonValue GetValue(string json)
        {
            JsonValueType valueType = JsonValueType.Unset;

            switch (this.Type)
            {
                case JsonTokenType.False:
                case JsonTokenType.True:
                    valueType = JsonValueType.Bool;
                    break;

                case JsonTokenType.Null:
                    valueType = JsonValueType.Null;
                    break;

                case JsonTokenType.Number:
                    valueType = JsonValueType.Number;
                    break;

                case JsonTokenType.String:
                    valueType = JsonValueType.String;
                    break;
            }

            return new JsonValue(valueType, this, json);
        }

        public string GetText(string json)
        {
            return json.Substring(this.Start, this.Length);
        }

        public string GetDecodedString(string json)
        {
            if (!this.IsString)
            {
                throw new InvalidOperationException($"JsonToken is not a String: {this.Type}");
            }

            StringBuilder value = new StringBuilder(this.Length);

            int cur = this.Start + 1;
            for (int end = this.Start + this.Length - 1; cur != -1 && cur < end;)
            {
                if (json[cur] == '\\')
                {
                    switch (cur + 1 < end ? json[cur + 1] : '\0')
                    {
                        case '\"':
                        case '\\':
                        case '/':
                            value.Append(json[cur + 1]);
                            cur += 2;
                            break;

                        case 'b':
                            value.Append('\b');
                            cur += 2;
                            break;

                        case 'f':
                            value.Append('\f');
                            cur += 2;
                            break;

                        case 'n':
                            value.Append('\n');
                            cur += 2;
                            break;

                        case 'r':
                            value.Append('\r');
                            cur += 2;
                            break;

                        case 't':
                            value.Append('\t');
                            cur += 2;
                            break;

                        case 'u':
                            if (cur + 5 < end)
                            {
                                string buffer = json.Substring(cur + 2, 4);
                                if (uint.TryParse(buffer, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint decoded))
                                {
                                    value.Append((char)decoded);
                                    cur += 6;
                                }
                                else
                                {
                                    cur = -1;
                                }
                            }
                            else
                            {
                                cur = -1;
                            }
                            break;

                        default:
                            cur = -1;
                            break;
                    }
                }
                else
                {
                    value.Append(json[cur]);
                    cur++;
                }
            }

            return (cur != -1) ? value.ToString() : null;
        }
    }
}
