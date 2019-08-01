using System;

namespace DevOps.Avatars
{
    internal interface IAvatarProvider
    {
        void ProvideAvatar(Uri uri, IAvatarSite site);
    }
}
