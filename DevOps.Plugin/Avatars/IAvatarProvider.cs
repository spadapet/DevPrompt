using System;

namespace DevOps.Avatars
{
    internal interface IAvatarProvider
    {
        void ProvideAvatar(Guid id, IAvatarSite site);
    }
}
