using System;
using System.Collections.Generic;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Plugins implement this to let the data serializer know about extra types to serialize
    /// </summary>
    public interface ISettingTypes
    {
        /// <summary>
        /// Saved with AppSettings
        /// </summary>
        IEnumerable<Type> SettingTypes { get; }

        /// <summary>
        /// Saved with AppSnapshot
        /// </summary>
        IEnumerable<Type> SnapshotTypes { get; }
    }
}
