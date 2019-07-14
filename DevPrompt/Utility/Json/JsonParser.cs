using System.Collections.Generic;
using System.Globalization;

namespace DevPrompt.Utility.Json
{
    internal class JsonParser
    {
        public static Api.IJsonValue Parse(string json)
        {
            JsonParser parser = new JsonParser(json);
            JsonValue value = parser.RootValue;
            return parser.context.GetInterface(value);
        }

        private JsonTokenizer tokenizer;
        private JsonValueContext context;

        private JsonParser(string json)
        {
            this.tokenizer = new JsonTokenizer(json);
            this.context = new JsonValueContext(json);
        }

        private JsonToken NextToken => this.tokenizer.NextToken;

        private JsonValue RootValue
        {
            get
            {
                try
                {
                    JsonToken token = this.NextToken;
                    if (!token.IsOpenCurly)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedObject);
                    }

                    return this.NextObject;
                }
                catch (JsonException ex)
                {
                    return new JsonValue(JsonValueType.Exception, ex, this.context);
                }
            }
        }

        public JsonValue NextObject
        {
            get
            {
                Dictionary<string, JsonValue> dict = new Dictionary<string, JsonValue>();

                for (JsonToken token = this.NextToken; !token.IsCloseCurly;)
                {
                    if (!token.IsString)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedKeyName);
                    }

                    string key = token.GetDecodedString(this.context.Json);
                    if (key == null || dict.ContainsKey(key))
                    {
                        throw new JsonException(token, (key == null)
                            ? Resources.JsonParser_InvalidStringToken
                            : string.Format(CultureInfo.CurrentCulture, Resources.JsonParser_ExpectedUniqueKey, key));
                    }

                    token = this.NextToken;
                    if (!token.IsColon)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedKeyColon);
                    }

                    token = this.NextToken;
                    dict.Add(key, this.GetValue(token));

                    token = this.NextToken;
                    if (!token.IsComma && !token.IsCloseCurly)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedCommaOrCurly);
                    }

                    if (token.IsComma)
                    {
                        token = this.NextToken;
                    }
                }

                return new JsonValue(JsonValueType.Dictionary, dict, this.context);
            }
        }

        public JsonValue NextArray
        {
            get
            {
                List<JsonValue> values = new List<JsonValue>();

                for (JsonToken token = this.NextToken; !token.IsCloseBracket;)
                {
                    values.Add(this.GetValue(token));

                    token = this.NextToken;
                    if (!token.IsComma && !token.IsCloseBracket)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedCommaOrBracket);
                    }

                    if (token.IsComma)
                    {
                        token = this.NextToken;
                    }
                }

                return new JsonValue(JsonValueType.Array, values, this.context);
            }
        }

        public JsonValue GetValue(JsonToken token)
        {
            JsonValue value = token.GetValue(this.context);

            if (value.IsType(JsonValueType.Unset))
            {
                if (token.IsOpenCurly)
                {
                    value = this.NextObject;
                }
                else if (token.IsOpenBracket)
                {
                    value = this.NextArray;
                }
                else
                {
                    throw new JsonException(token, Resources.JsonParser_ExpectedValue);
                }
            }

            return value;
        }
    }
}
