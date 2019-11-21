using System;

namespace DevPrompt.Api
{
    /// <summary>
    /// This holds a tab that's already been added to a workspace
    /// </summary>
    public interface ITabHolder
    {
        Guid Id { get; }
        ActiveState ActiveState { get; set; }
        bool CreatedTab { get; }
        ITab Tab { get; }
    }
}
