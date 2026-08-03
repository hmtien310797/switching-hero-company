using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Item UI dùng chung cho các list view PvP (hero selection, buff loadout). Mỗi item = 1 Button
    /// + label (+ optional icon/selected highlight). View instantiate từ prefab (Addressable/scene).
    /// </summary>
    public class PvpOptionItemView : MonoBehaviour
    {
        [SerializeField] public Button button;
        [SerializeField] public TMP_Text label;
        [SerializeField] public Image icon;
        [SerializeField] public GameObject selectedHighlight;
        [SerializeField] public GameObject disabledOverlay;
    }
}
