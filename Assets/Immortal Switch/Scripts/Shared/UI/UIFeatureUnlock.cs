using Common;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    [RequireComponent(typeof(Button))]
    public class UIFeatureUnlock : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private EFeatureUnlockType unlockType;

        [SerializeField]
        private Button btn;

        [SerializeField]
        private GameObject locked;

        // --- Private Fields ---
        private DynamicHeroesGlobalSpecificationsFeatureUnlockConfigRow _cfg;
        private bool _isUnlocked;

        private void Awake()
        {
            if (UserDataCache.Instance != null)
            {
                UserDataCache.Instance.OnExpChanged += RefreshView;
            }
        }

        private void OnDestroy()
        {
            if (UserDataCache.Instance != null)
            {
                UserDataCache.Instance.OnExpChanged -= RefreshView;
            }
        }

        private void OnEnable()
        {
            RefreshView();
        }

        private void RefreshView()
        {
            var cfg = DatabaseManager.Instance.TryCheckFeatureUnlockConfig(unlockType, out var unlocked);

            if (cfg == null)
            {
                return;
            }

            _cfg = cfg;
            _isUnlocked = unlocked;
            btn.interactable = _isUnlocked;

            locked.SetActive(!_isUnlocked);
        }

        /// <summary>
        /// Hiển thị lý do bị khóa mà không xóa các listener gốc của Button.
        /// Khi Button không interactable, callback onClick của tính năng sẽ không được gọi.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isUnlocked)
            {
                OnClickLock();
            }
        }

        private void OnClickLock()
        {
            if (_cfg != null)
            {
                UIManager.Instance.ShowToast($"Bạn cần đạt level {_cfg.requiredLevel} để mở khoá tính năng này!!!");
            }
        }
    }
}