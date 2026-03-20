using System;

namespace Immortal_Switch.Scripts.UI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UIAddressAttribute : Attribute
    {
        public string Key;

        public UIAddressAttribute(string key)
        {
            Key = key;
        }
    }
}