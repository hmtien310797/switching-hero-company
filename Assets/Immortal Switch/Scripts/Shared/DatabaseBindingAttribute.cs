using System;

namespace Immortal_Switch.Scripts.Shared
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DatabaseBindingAttribute : Attribute
    {
        public bool Required { get; }

        public DatabaseBindingAttribute(bool required = true)
        {
            Required = required;
        }
    }
}