namespace DevPrompt.Api
{
    public interface IJsonParser
    {
        IJsonValue Parse(string json);
        dynamic ParseAsDynamic(string json);
        T ParseAsType<T>(string json);
    }
}
