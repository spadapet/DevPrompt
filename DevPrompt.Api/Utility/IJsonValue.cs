using System;
using System.Collections.Generic;

namespace DevPrompt.Api
{
    /// <summary>
    /// The value accessor methods will throw if the type is wrong.
    /// For example, don't ever call Double unless IsDouble is true.
    /// </summary>
    public interface IJsonValue
    {
        bool IsArray { get; }
        bool IsBool { get; }
        bool IsDictionary { get; }
        bool IsDouble { get; }
        bool IsException { get; }
        bool IsInt { get; }
        bool IsNull { get; }
        bool IsString { get; }
        bool IsUnset { get; }

        IReadOnlyList<IJsonValue> Array { get; }
        bool Bool { get; }
        IReadOnlyDictionary<string, IJsonValue> Dictionary { get; }
        double Double { get; }
        IJsonException Exception { get; }
        int Int { get; }
        string String { get; }
        object Value { get; }

        /// <summary>
        /// Safe array accessor that return an unset value instead of throwing
        /// </summary>
        IJsonValue this[int index] { get; }

        /// <summary>
        /// Safe dictionary accessor that returns an unset value instead of throwing
        /// </summary>
        /// <param name="path">Can be a simple key or a path like "foo.bar[3].baz" or "[2].foo"</param>
        IJsonValue this[string path] { get; }
    }
}
