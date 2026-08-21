using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.GameSetting.Views.UI;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.Helper;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GameSetting.Views.Layouts
{
    public class UISettingMainLayout : MonoBehaviour
    {
        [Header("Profile references")]
        [SerializeField]
        private TextMeshProUGUI txtName;

        [SerializeField]
        private TextMeshProUGUI txtUid;

        [SerializeField]
        private Button btnCopyUid;

        [SerializeField]
        private Button btnLogout;

        [SerializeField]
        private Button btnDeleteAccount;

        [Header("Link references")]
        [SerializeField]
        private Button btnLinkClaim;

        [SerializeField]
        private Button btnLink;

        [PreviewField]
        [SerializeField]
        private Sprite sprGgUnlink;

        [PreviewField]
        [SerializeField]
        private Sprite sprGgLinked;

        [PreviewField]
        [SerializeField]
        private Sprite sprAppleUnlink;

        [PreviewField]
        [SerializeField]
        private Sprite sprAppleLinked;

        [SerializeField]
        private GameObject goLinkClaimed;

        [SerializeField]
        private Image imgAccountLinked;

        [SerializeField]
        private Image imgAccountUnlink;

        [Header("Language references")]
        [SerializeField]
        private RectTransform languageContainer;

        [SerializeField]
        private UISettingLanguageItem languagePrefab;

        [Header("Other references")]
        [SerializeField]
        private Button btnTerms;

        [SerializeField]
        private Button btnPolicy;

        [SerializeField]
        private Button btnSupport;

        [SerializeField]
        private Button btnGiftCode;

        // --- Private Fields ---
        private SimpleUIPool<UISettingLanguageItem> _pools;

        private void Awake()
        {
            btnLinkClaim.onClick.AddListener(OnClickLinkClaim);
            btnLink.onClick.AddListener(OnClickLink);

            btnCopyUid.onClick.AddListener(OnClickCopyUid);
            btnLogout.onClick.AddListener(OnClickLogout);
            btnDeleteAccount.onClick.AddListener(OnClickDeleteAccount);
            btnTerms.onClick.AddListener(OnClickTerms);
            btnPolicy.onClick.AddListener(OnClickPolicy);
            btnSupport.onClick.AddListener(OnClickSupport);
            btnGiftCode.onClick.AddListener(OnClickGiftCode);
        }

        private void OnClickLink()
        {
            LinkAccountAsync().Forget();
        }

        private async UniTaskVoid LinkAccountAsync()
        {
            var linked = await SettingManager.Instance.LinkAccountAsync();

            if (linked)
            {
                SetLinked(true);
                SetLinkedClaimed(true, UserDataCache.Instance.LinkRewardClaimed);
            }
        }

        private void OnClickLinkClaim()
        {
            ClaimLinkRewardAsync().Forget();
        }

        private async UniTaskVoid ClaimLinkRewardAsync()
        {
            var claimed = await SettingManager.Instance.ClaimLinkRewardAsync();

            if (claimed)
            {
                SetLinkedClaimed(true, true);
            }
        }

        private void OnEnable()
        {
            RefreshViews();
        }

        private void OnDestroy()
        {
            btnLinkClaim.onClick.RemoveListener(OnClickLinkClaim);
            btnLink.onClick.RemoveListener(OnClickLink);

            btnCopyUid.onClick.RemoveListener(OnClickCopyUid);
            btnLogout.onClick.RemoveListener(OnClickLogout);
            btnDeleteAccount.onClick.RemoveListener(OnClickDeleteAccount);
            btnTerms.onClick.RemoveListener(OnClickTerms);
            btnPolicy.onClick.RemoveListener(OnClickPolicy);
            btnSupport.onClick.RemoveListener(OnClickSupport);
            btnGiftCode.onClick.RemoveListener(OnClickGiftCode);
        }

        public void Bind(bool isLinkClaimed, bool isLinked)
        {
            SetLinked(isLinked);
            SetLinkedClaimed(isLinked, isLinkClaimed);
            RefreshLanguage();
        }

        private void RefreshLanguage()
        {
            _pools ??= new SimpleUIPool<UISettingLanguageItem>(languagePrefab, languageContainer);

            var languages = DatabaseManager.Instance.GetLanguagesReleased();

            for (var index = 0; index < languages.Count; index++)
            {
                var entry = languages[index];
                var isSelected = SettingManager.Instance.CurrentSetting.LangCode == entry.langCode;
                var clone = _pools.Get(index);

                clone.Bind(entry.name, entry.langCode, OnChangeLanguage);
                clone.SetSelected(isSelected);
            }

            // hide cac object ko su dung
            _pools.ReleaseFrom(languages.Count);
        }

        private void OnChangeLanguage(string langCode)
        {
            SettingManager.Instance.SetLangCode(langCode);
            RefreshLanguage();
        }

        public void SetLinkedClaimed(bool isLinked, bool isLinkClaimed)
        {
            if (isLinked && isLinkClaimed)
            {
                goLinkClaimed.SetActive(true);
                btnLinkClaim.gameObject.SetActive(false);
            }
            else if (isLinked)
            {
                goLinkClaimed.SetActive(false);
                btnLinkClaim.gameObject.SetActive(true);
                btnLinkClaim.interactable = true;
            }
            else
            {
                goLinkClaimed.SetActive(false);
                btnLinkClaim.gameObject.SetActive(true);
                btnLinkClaim.interactable = false;
            }
        }

        public void SetLinked(bool isLinked)
        {
            if (isLinked)
            {
                imgAccountLinked.gameObject.SetActive(true);
                imgAccountUnlink.gameObject.SetActive(false);
            }
            else
            {
                imgAccountLinked.gameObject.SetActive(false);
                imgAccountUnlink.gameObject.SetActive(true);
            }
        }

        private void RefreshViews()
        {
            txtName.text = UserDataCache.Instance.DisplayName;
            txtUid.text = UserDataCache.Instance.Uid;

#if UNITY_EDITOR || UNITY_ANDROID
            imgAccountLinked.sprite = sprGgLinked;
            imgAccountUnlink.sprite = sprGgUnlink;

            btnGiftCode.gameObject.SetActive(false);
#elif UNITY_IOS
            imgAccountLinked.sprite = sprAppleLinked;
            imgAccountUnlink.sprite = sprAppleUnlink;

            btnGiftCode.gameObject.SetActive(false);
#endif
        }

        private void OnClickGiftCode()
        {
            OpenUrl("https://google.com");
        }

        private void OnClickSupport()
        {
            OpenUrl("https://google.com");
        }

        private void OnClickPolicy()
        {
            OpenUrl("https://google.com");
        }

        private void OnClickTerms()
        {
            OpenUrl("https://google.com");
        }

        private void OnClickDeleteAccount()
        {
            UIManager.Instance
                .OpenPopupAsync<PopupConfirmView>(new PopupConfirmArgs(
                    LocalizationManager.GetText(LocalizationKeys.UI_DELETE_ACCOUNT),
                    LocalizationManager.GetText(LocalizationKeys.UI_DELETE_ACCOUNT_DESC),
                    SettingManager.Instance.DeleteAccount,
                    showToggleDoNotShowAgain: false
                ))
                .Forget();
        }

        private void OnClickLogout()
        {
            SettingManager.Instance.Logout();
        }

        private void OnClickCopyUid()
        {
            if (!string.IsNullOrEmpty(txtUid.text))
            {
                ClipboardHelper.Copy(txtUid.text);
            }
        }

        private void OpenUrl(string url)
        {
            Application.OpenURL("https://google.com");
        }
    }
}