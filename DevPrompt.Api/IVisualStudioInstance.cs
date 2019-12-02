namespace DevPrompt.Api
{
    public interface IVisualStudioInstance
    {
        string Name { get; }
        string Id { get; }
        string Path { get; }
        string ProductPath { get; }
        string Version { get; }
        string Channel { get; }
        string DisplayName { get; }
    }
}
