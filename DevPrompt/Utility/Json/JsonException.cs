using System;
using System.Globalization;

namespace DevPrompt.Utility.Json
{
    internal class JsonException : Exception, Api.IJsonException
    {
        public JsonToken ErrorToken { get; }
        private string message;

        public JsonException(JsonToken errorToken, string message = null)
        {
            this.ErrorToken = errorToken;
            this.message = message;
        }

        string Api.IJsonException.Message => this.Message;
        string Api.IJsonException.TokenType => this.ErrorToken.Type.ToString();
        int Api.IJsonException.TokenStart => this.ErrorToken.Start;
        int Api.IJsonException.TokenLength => this.ErrorToken.Length;

        public override string Message
        {
            get
            {
                if (string.IsNullOrEmpty(this.message))
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.JsonException_Message,
                        this.ErrorToken.Start, // {0}
                        this.ErrorToken.Type); // {1}
                }
                else
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.JsonException_Message2,
                        this.ErrorToken.Start, // {0}
                        this.ErrorToken.Type, // {1}
                        this.message); // {2}
                }
            }
        }
    }
}
