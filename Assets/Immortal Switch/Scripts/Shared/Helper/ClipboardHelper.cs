using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.Helper
{
    public class ClipboardHelper
    {
        public static void Copy(string text)
        {
            GUIUtility.systemCopyBuffer = text;

            UIManager.Instance.ShowToast($"Đã copy: {text}");
        }

        public static string Paste()
        {
            return GUIUtility.systemCopyBuffer;
        }
    }
}