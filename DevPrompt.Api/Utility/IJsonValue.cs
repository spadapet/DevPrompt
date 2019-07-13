using System.Collections.Generic;

namespace DevPrompt.Api
{
    public interface IJsonValue : IReadOnlyList<IJsonValue>, IReadOnlyDictionary<string, IJsonValue>
    {
        bool IsArray { get; }
        bool IsBool { get; }
        bool IsDictionary { get; }
        bool IsNull { get; }
        bool IsNumber { get; }
        bool IsString { get; }
        bool IsUnset { get; }

        bool Bool { get; }
        int Int { get; }
        double Double { get; }
        string String { get; }
        object Value { get; }
        dynamic Dynamic { get; }
    }
}
