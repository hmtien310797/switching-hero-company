#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp;
using UnityEditor;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Editor
{
    public static class PvpPlaymodeMenu
    {
        [MenuItem("Tools/PvP/Open PvP Main (Playmode)")]
        public static void OpenPvpMain()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PvP] Enter Play mode first (the real battle runs at runtime).");
                return;
            }

            PvpEntry.OpenPvpMain();
        }

        [MenuItem("Tools/PvP/Quick Battle (No UI)")]
        public static void QuickBattleNoUi()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PvP] Enter Play mode first — run in a scene with player hero loaded (e.g. MainBattleScene).");
                return;
            }

            PvpQuickBattle.StartAsync(openHud: false).Forget();
        }

        [MenuItem("Tools/PvP/Quick Battle (With HUD)")]
        public static void QuickBattleWithHud()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PvP] Enter Play mode first — run in a scene with player hero loaded (e.g. MainBattleScene).");
                return;
            }

            PvpQuickBattle.StartAsync(openHud: true).Forget();
        }
    }
}
#endif
