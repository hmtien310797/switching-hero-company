using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.PlayerSystem.Views.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.PlayerSystem.Views
{
    public class ProfileView : AnimatedUIView
    {
        [Header("References")] [SerializeField]
        private TMP_Text txtName;

        [SerializeField] private Button btnRename;
        [SerializeField] private Button btnClose;
        [SerializeField] private UIProfileRenamePopup uiRename;

        [Header("Options")] [SerializeField] private RectTransform optionContainer;
        [SerializeField] private UIProfileTitleOption optionTitleFinal;
        [SerializeField] private UIProfileRowOption optionRowFinal1;
        [SerializeField] private UIProfileRowOption optionRowFinal2;
        [SerializeField] private UIProfileRowOption optionRowFinal3;

        [SerializeField] private UIProfileTitleOption optionTitleAll;
        [SerializeField] private UIProfileRowOption optionRowTemplate;

        private void Awake()
        {
            btnRename.onClick.AddListener(OnClickRename);
            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickRename()
        {
            uiRename.gameObject.SetActive(true);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<ProfileView>().Forget();
        }

        public void Bind()
        {
        }
    }
}