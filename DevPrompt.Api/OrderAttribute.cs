using System;
using System.ComponentModel;
using System.Composition;

namespace DevPrompt.Api
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class OrderAttribute : Attribute
    {
        [DefaultValue(Constants.NormalPriority)]
        public int Order { get; set; }

        public OrderAttribute()
            : this(Constants.NormalPriority)
        {
        }

        public OrderAttribute(int order)
        {
            this.Order = order;
        }
    }
}
