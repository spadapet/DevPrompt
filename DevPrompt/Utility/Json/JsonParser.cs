using System.Collections.Generic;
using System.Composition;
using System.Globalization;

namespace DevPrompt.Utility.Json
{
    /// <summary>
    /// JSON parser available for plugins to Import
    /// </summary>
    [Export(typeof(Api.IWorkspaceProvider))]
    internal class JsonParserExport : Api.IJsonParser
    {
        Api.IJsonValue Api.IJsonParser.Parse(string json)
        {
            return JsonParser.Parse(json);
        }

        dynamic Api.IJsonParser.ParseAsDynamic(string json)
        {
            return JsonParser.ParseAsDynamic(json);
        }

        T Api.IJsonParser.ParseAsType<T>(string json)
        {
            return JsonParser.ParseAsType<T>(json);
        }
    }

    internal class JsonParser
    {
        public static Api.IJsonValue Parse(string json)
        {
            JsonParser parser = new JsonParser(json);
            JsonValueData value = parser.RootValue;
            return parser.context.GetValue(value);
        }

        public static dynamic ParseAsDynamic(string json)
        {
            return JsonParser.Parse(json).Dynamic;
        }

        public static T ParseAsType<T>(string json)
        {
            return JsonParser.Parse(json).Convert<T>();
        }

        private JsonTokenizer tokenizer;
        private JsonContext context;
        private HashSet<string> keyCache;
        private Stack<Dictionary<string, JsonValueData>> parseDicts;
        private Stack<List<JsonValueData>> parseArrays;
        private const int bufferSize = 32;

        private JsonParser(string json)
        {
            this.tokenizer = new JsonTokenizer(json);
            this.context = new JsonContext(json);
            this.keyCache = new HashSet<string>(JsonParser.bufferSize);
            this.parseDicts = new Stack<Dictionary<string, JsonValueData>>(JsonParser.bufferSize);
            this.parseArrays = new Stack<List<JsonValueData>>(JsonParser.bufferSize);
        }

        private JsonToken NextToken => this.tokenizer.NextToken;

        private JsonValueData RootValue
        {
            get
            {
                JsonToken token = this.NextToken;
                if (!token.IsOpenCurly)
                {
                    throw new JsonException(token, Resources.JsonParser_ExpectedObject);
                }

                return this.NextObject;
            }
        }

        public JsonValueData NextObject
        {
            get
            {
                Dictionary<string, JsonValueData> parseDict = (this.parseDicts.Count > 0)
                    ? this.parseDicts.Pop()
                    : new Dictionary<string, JsonValueData>(JsonParser.bufferSize);

                for (JsonToken token = this.NextToken; !token.IsCloseCurly;)
                {
                    if (!token.IsString)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedKeyName);
                    }

                    string key = JsonTokenizer.DecodeString(this.context.Json, token);
                    if (key == null)
                    {
                        throw new JsonException(token, Resources.JsonParser_InvalidStringToken);
                    }

                    if (parseDict.ContainsKey(key))
                    {
                        throw new JsonException(token, string.Format(CultureInfo.CurrentCulture, Resources.JsonParser_ExpectedUniqueKey, key));
                    }

                    if (!this.keyCache.TryGetValue(key, out string actualKey))
                    {
                        actualKey = key;
                        this.keyCache.Add(key);
                    }

                    token = this.NextToken;
                    if (!token.IsColon)
                    {
                        throw new JsonException(token, Resources.JsonParser_ExpectedKeyColon);
                    }

                    token = this.NextToken;
                    parseDict.Add(actualKey, this.GetValue(token));

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

                JsonValues actualDict = (parseDict.Count > 0) ? new JsonValues(parseDict, this.context) : this.context.EmptyDictionary;
                parseDict.Clear();
                this.parseDicts.Push(parseDict);

                return new JsonValueData(JsonValueType.Dictionary, JsonToken.None, actualDict);
            }
        }

        public JsonValueData NextArray
        {
            get
            {
                List<JsonValueData> parseArray = (this.parseArrays.Count > 0)
                    ? this.parseArrays.Pop()
                    : new List<JsonValueData>(JsonParser.bufferSize);

                for (JsonToken token = this.NextToken; !token.IsCloseBracket;)
                {
                    parseArray.Add(this.GetValue(token));

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

                JsonValues actualArray = (parseArray.Count > 0) ? new JsonValues(parseArray, this.context) : this.context.EmptyArray;
                parseArray.Clear();
                this.parseArrays.Push(parseArray);

                return new JsonValueData(JsonValueType.Array, JsonToken.None, actualArray);
            }
        }

        public JsonValueData GetValue(JsonToken token)
        {
            JsonValueType valueType;

            switch (token.Type)
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

                case JsonTokenType.OpenCurly:
                    return this.NextObject;

                case JsonTokenType.OpenBracket:
                    return this.NextArray;

                default:
                    throw new JsonException(token, Resources.JsonParser_ExpectedValue);
            }

            return new JsonValueData(valueType, token, this.context.EmptyArray);
        }
    }
}
