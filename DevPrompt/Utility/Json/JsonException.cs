using System;
using System.Globalization;

namespace DevPrompt.Utility.Json
{
    internal class JsonException : Exception
    {
        public JsonToken ErrorToken { get; }
        private string message;

        public JsonException(JsonToken errorToken, string message = null)
        {
            this.ErrorToken = errorToken;
            this.message = message;
        }

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
