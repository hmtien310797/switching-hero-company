using Common;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Tutorial;
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
            UserDataCache.Instance.OnExpChanged += RefreshView;
        }

        private void OnDestroy()
        {
            UserDataCache.Instance.OnExpChanged -= RefreshView;
        }

        private void OnEnable()
        {
            RefreshView();
        }

        private void RefreshView()
        {
            var cfg = DatabaseManager.Instance.GetFeatureUnlockConfig(unlockType);

            if (cfg == null)
            {
                return;
            }

            var playerLevelInfo = DatabaseManager.Instance.GetLevelByTotalExp(UserDataCache.Instance.Exp);

            _cfg = cfg;
            _isUnlocked = playerLevelInfo.level >= cfg.requiredLevel;
            btn.interactable = _isUnlocked;

            locked.SetActive(!_isUnlocked);

            if (_isUnlocked &&
                _cfg.tutorialStepId > 0)
            {
                TutorialManager.Instance.TryGuide(_cfg.tutorialStepId).Forget();
            }
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