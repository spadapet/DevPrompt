using System.Collections.Generic;

namespace DevPrompt.Api
{
    public interface IJsonValue
    {
        bool IsArray { get; }
        bool IsBool { get; }
        bool IsDictionary { get; }
        bool IsDouble { get; }
        bool IsInt { get; }
        bool IsNull { get; }
        bool IsString { get; }
        bool IsValid { get; }

        IReadOnlyList<IJsonValue> Array { get; }
        bool Bool { get; }
        IReadOnlyDictionary<string, IJsonValue> Dictionary { get; }
        double Double { get; }
        int Int { get; }
        string String { get; }

        /// <summary>
        /// Safely returns the appropriate type or null instead of throwing
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Safe array accessor that return an invalid value instead of throwing
        /// </summary>
        IJsonValue this[int index] { get; }

        /// <summary>
        /// Safe dictionary accessor that returns an invalid value instead of throwing
        /// </summary>
        IJsonValue this[string key] { get; }
    }
}
