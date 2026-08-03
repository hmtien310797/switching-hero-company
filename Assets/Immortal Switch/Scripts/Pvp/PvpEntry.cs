using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp
{
    /// <summary>
    /// Runtime entry helper cho PvP flow. Gọi <see cref="OpenPvpMain"/> để mở PvpMainView (screen 01)
    /// từ button/key bất kỳ. Yêu cầu UIManager đã khởi tạo (có Canvas + Addressable views đã scaffold).
    /// </summary>
    public static class PvpEntry
    {
        public static void OpenPvpMain()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[PvP] UIManager not ready — ensure the scene has UIManager (it auto-creates).");
                return;
            }

            UIManager.Instance.OpenPopupAsync<PvpMainView>().Forget();
        }
    }
}
