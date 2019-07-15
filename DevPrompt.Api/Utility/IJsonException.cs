namespace DevPrompt.Api
{
    public interface IJsonException
    {
        string Message { get; }
        string TokenType { get; }
        int TokenStart { get; }
        int TokenLength { get; }
    }
}
