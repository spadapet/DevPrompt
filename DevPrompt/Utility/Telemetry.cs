using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DevPrompt.Utility
{
    internal class Telemetry : IDisposable, Api.ITelemetry
    {
        private TelemetryConfiguration config;
        private TelemetryClient client;

        public Telemetry()
        {
            this.config = new TelemetryConfiguration("5c831494-8408-4371-922d-497edc9a7d64");
#if DEBUG
            this.config.DisableTelemetry = true;
#endif
            this.client = new TelemetryClient(this.config);
            this.client.Context.Session.Id = Guid.NewGuid().ToString();
            this.client.Context.Component.Version = this.GetType().Assembly.GetName().Version.ToString();
            this.client.Context.User.Id = Telemetry.GetUserId();
        }

        public void Dispose()
        {
            this.client.Flush();
            this.config.Dispose();
        }

        private static string GetUserId()
        {
            try
            {
                const string valueName = "InstallId";

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\DevPrompt", writable: true))
                {
                    if (key != null)
                    {
                        if (!(key.GetValue(valueName) is string id) || !Guid.TryParse(id, out _))
                        {
                            id = Guid.NewGuid().ToString();
                            key.SetValue(valueName, id);
                        }

                        return id;
                    }
                }
            }
            catch
            {
                // No user ID
            }

            return null;
        }

        public void TrackEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (properties == null)
            {
                this.client.TrackEvent(eventName);
                return;
            }

            Dictionary<string, string> eventProperties = new Dictionary<string, string>();
            Dictionary<string, double> eventMetrics = new Dictionary<string, double>();

            foreach (KeyValuePair<string, object> pair in properties.Where(p => !string.IsNullOrEmpty(p.Key) && p.Value != null))
            {
                if (!(pair.Value is IConvertible convertible))
                {
                    if (pair.Value is TimeSpan timeSpan)
                    {
                        eventMetrics[pair.Key] = timeSpan.TotalSeconds;
                    }
                    else if (pair.Value.ToString() is string stringValue)
                    {
                        eventProperties[pair.Key] = stringValue;
                    }

                    continue;
                }

                switch (convertible.GetTypeCode())
                {
                    case TypeCode.DateTime:
                        eventProperties[pair.Key] = ((DateTime)pair.Value).ToString("u", CultureInfo.InvariantCulture);
                        break;

                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.String:
                    case TypeCode.Object:
                        try
                        {
                            if (convertible.ToString(CultureInfo.InvariantCulture) is string stringValue)
                            {
                                eventProperties[pair.Key] = stringValue;
                            }
                        }
                        catch
                        {
                            // ignore it
                        }
                        break;

                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        if (pair.Value.GetType().IsEnum)
                        {
                            eventProperties[pair.Key] = Enum.GetName(pair.Value.GetType(), pair.Value);
                        }
                        else
                        {
                            try
                            {
                                if (convertible.ToDouble(CultureInfo.InvariantCulture) is double doubleValue)
                                {
                                    eventMetrics[pair.Key] = doubleValue;
                                }
                            }
                            catch
                            {
                                // ignore it
                            }
                        }
                        break;
                }
            }

            this.client.TrackEvent(eventName, (eventProperties.Count > 0) ? eventProperties : null, (eventMetrics.Count > 0) ? eventMetrics : null);
        }
    }
}
