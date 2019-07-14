namespace DevPrompt.Utility
{
    internal static class HashUtility
    {
        public static int HashSubstring(string str, int start, int length)
        {
            int end = start + length;
            int pos = start;
            int hash1 = 5381;
            int hash2 = 5381;

            for (; pos + 1 < end; pos += 2)
            {
                unchecked
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[pos];
                    hash2 = ((hash2 << 5) + hash2) ^ str[pos + 1];
                }
            }

            if (pos < end)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[pos];
            }

            int hash = hash1 + (hash2 * 1566083941);
            return hash;
        }
    }
}
