namespace DevPrompt.Utility
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
    }

    internal class JsonTokenizer
    {
    }
}
