using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.GameSetting.Views.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Helper;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
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

        [SerializeField]
        private GameObject goLinkClaimed;

        [SerializeField]
        private GameObject goAccountLinked;

        [SerializeField]
        private GameObject goAccountUnlink;

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
        private List<UISettingLanguageItem> _languages = new();
        private Action _onGgLink;
        private Action _onLinkClaim;

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
            _onGgLink?.Invoke();
        }

        private void OnClickLinkClaim()
        {
            _onLinkClaim?.Invoke();
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

        public void Bind(
            bool isLinkClaimed, bool isLinked,
            Action onGgLink, Action onLinkClaim
        )
        {
            _onGgLink = onGgLink;
            _onLinkClaim = onLinkClaim;

            SetLinked(isLinked);
            SetLinkedClaimed(isLinked, isLinkClaimed);
            RefreshLanguage();
        }

        private void RefreshLanguage()
        {
            var languages = DatabaseManager.Instance.GetLanguagesReleased();

            for (var index = 0; index < languages.Count; index++)
            {
                var entry = languages[index];
                var isSelected = SettingManager.Instance.CurrentSetting.LangCode == entry.langCode;

                if (_languages.Count > index)
                {
                    var clone = _languages[index];
                    clone.gameObject.SetActive(true);
                    clone.Bind(entry.nameNative, entry.langCode, OnChangeLanguage);
                    clone.SetSelected(isSelected);
                }
                else
                {
                    var clone = Instantiate(languagePrefab, languageContainer);
                    clone.Bind(entry.nameNative, entry.langCode, OnChangeLanguage);
                    clone.SetSelected(isSelected);
                    _languages.Add(clone);
                }
            }

            // hide cac object ko su dung
            for (int i = languages.Count; i < _languages.Count; i++)
            {
                _languages[i].gameObject.SetActive(false);
            }
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
                goAccountLinked.SetActive(true);
                goAccountUnlink.SetActive(false);
            }
            else
            {
                goAccountLinked.SetActive(false);
                goAccountUnlink.SetActive(true);
            }
        }

        private void RefreshViews()
        {
            txtName.text = UserDataCache.Instance.DisplayName;
            txtUid.text = UserDataCache.Instance.Uid;
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
                    "Xoá tài khoản",
                    "Hành động này sẽ xoá vĩnh viễn tài khoản và toàn bộ dữ liệu.\nBạn có chắc chắn muốn tiếp tục?",
                    () => SettingManager.Instance.DeleteAccount(),
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