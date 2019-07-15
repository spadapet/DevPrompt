using System.Diagnostics;

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

        public static JsonToken None => default;
        public bool IsString => this.Type == JsonTokenType.String;
        public bool IsComma => this.Type == JsonTokenType.Comma;
        public bool IsColon => this.Type == JsonTokenType.Colon;
        public bool IsOpenCurly => this.Type == JsonTokenType.OpenCurly;
        public bool IsCloseCurly => this.Type == JsonTokenType.CloseCurly;
        public bool IsCloseBracket => this.Type == JsonTokenType.CloseBracket;
    }
}
