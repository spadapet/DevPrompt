using DevPrompt.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Windows;
using System.Windows.Input;

namespace JSONTools.UI.ViewModels
{
    internal class ToolsVM : PropertyNotifier
    {
        private string input;
        private string output;

        public string Input
        {
            set
            {
                this.SetPropertyValue(ref this.input, value);
            }
        }

        public void OnValidate()
        {
            try
            {
                object obj = JToken.Parse(this.input);
                this.Output = "Valid";
            }
            catch (Exception)
            {
                this.Output = "Error";
            }
        }

        public void OnPrettify()
        {
            try
            {
                dynamic parsedJson = JsonConvert.DeserializeObject(this.input);
                this.Output = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch (Exception)
            {
                this.Output = "Error";
            }
        }

        public void OnStringify()
        {
            try
            {
                dynamic parsedJson = JsonConvert.DeserializeObject(this.input);
                this.Output = JsonConvert.SerializeObject(parsedJson);
            }
            catch (Exception)
            {
                this.Output = "Error";
            }
        }

        public string Output
        {
            get => this.output ?? string.Empty;

            private set
            {
                this.SetPropertyValue(ref this.output, value);
            }
        }
    }
}
