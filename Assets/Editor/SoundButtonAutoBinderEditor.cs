#if UNITY_EDITOR
using Immortal_Switch.Scripts.Sound;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Editor.Sound
{
    public static class SoundButtonAutoBinderEditor
    {
        [MenuItem("Tools/Immortal Switch/Sound/Add SoundButton To Selected UI")]
        private static void AddSoundButtonToSelectedUI()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                Debug.LogWarning("[SoundButtonAutoBinder] Please select a UI root object.");
                return;
            }

            Button[] buttons = selected.GetComponentsInChildren<Button>(true);

            int addedCount = 0;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button.GetComponent<SoundButton>() != null)
                {
                    continue;
                }

                Undo.AddComponent<SoundButton>(button.gameObject);
                addedCount++;
            }

            Debug.Log($"[SoundButtonAutoBinder] Added SoundButton to {addedCount} button(s).");
        }
    }
}
#endif
