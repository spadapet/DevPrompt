using System;
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
        /// Returns a value of the the appropriate type
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Returns a dynamic object for runtime binding
        /// </summary>
        dynamic Dynamic { get; }

        /// <summary>
        /// Safe array accessor that return an invalid value instead of throwing
        /// </summary>
        IJsonValue this[int index] { get; }

        /// <summary>
        /// Safe dictionary accessor that returns an invalid value instead of throwing
        /// </summary>
        IJsonValue this[string key] { get; }

        /// <summary>
        /// Converts the value to any type, and throws an exception on failure
        /// </summary>
        T Convert<T>();

        /// <summary>
        /// Tries to convert the value to any type
        /// </summary>
        bool TryConvert<T>(out T value);

        /// <summary>
        /// Converts the value to any type, and throws an exception on failure
        /// </summary>
        object Convert(Type type);

        /// <summary>
        /// Tries to convert the value to any type
        /// </summary>
        bool TryConvert(Type type, out object value);
    }
}
