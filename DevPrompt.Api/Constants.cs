using System;

namespace DevPrompt.Api
{
    public static class Constants
    {
        public static readonly Guid ProcessWorkspaceId = new Guid("f33d6443-849b-4a2c-abff-a1a9eda4a58e");

        public const int HighestPriority = 1000;
        public const int HigherPriority = 2000;
        public const int HighPriority = 3000;
        public const int NormalPriority = 4000;
        public const int LowPriority = 5000;
        public const int LowerPriority = 6000;
        public const int LowestPriority = 7000;
    }
}
