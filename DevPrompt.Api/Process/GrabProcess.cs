using System;

namespace DevPrompt.Api
{
    public struct GrabProcess : IEquatable<GrabProcess>
    {
        public int Id { get; }
        public string Name { get; }

        public GrabProcess(int id, string name)
        {
            this.Id = id;
            this.Name = name ?? string.Empty;
        }

        public GrabProcess(string name)
        {
            int id = 0;
            if (!string.IsNullOrEmpty(name) && name.StartsWith("[", StringComparison.Ordinal))
            {
                int end = name.IndexOf(']', 1);
                if (end != -1 && int.TryParse(name.Substring(1, end - 1), out int tempId))
                {
                    id = tempId;
                }
            }

            this.Id = id;
            this.Name = name ?? string.Empty;
        }

        public bool Equals(GrabProcess other)
        {
            return this.Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is GrabProcess other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
