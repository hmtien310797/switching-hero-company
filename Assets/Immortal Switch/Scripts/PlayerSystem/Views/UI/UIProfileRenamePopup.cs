using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Equipment.UIRuntime;
using Immortal_Switch.Scripts.PlayerSystem.Views;
using Immortal_Switch.Scripts.UI;
using Nakama;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.PlayerSystem.Views.UI
{
    public class UIProfileRenamePopup : BaseUIPopup
    {
        [Header("References")] [SerializeField]
        private TMP_Text txtPrice;

        [SerializeField] private TMP_InputField inputName;

        [SerializeField] private Button btnConfirm;

        private bool isRenaming;

        private void Awake()
        {
            BindButtons();
        }

        protected override void BindButtons()
        {
            base.BindButtons();
            btnConfirm.onClick.AddListener(OnConfirm);
        }

        private void OnConfirm()
        {
            if (isRenaming)
                return;

            var newName = inputName != null ? inputName.text.Trim() : null;
            if (string.IsNullOrEmpty(newName))
            {
                UIManager.Instance.ShowToast("Vui lòng nhập tên");
                return;
            }

            if (newName.Length < 2 || newName.Length > 20)
            {
                UIManager.Instance.ShowToast("Tên phải từ 2-20 ký tự");
                return;
            }

            RenameAsync(newName).Forget();
        }

        private async UniTaskVoid RenameAsync(string newName)
        {
            isRenaming = true;
            btnConfirm.interactable = false;

            try
            {
                var response = await NakamaClient.Instance.RenamePlayerAsync(newName);

                UserDataCache.Instance.DisplayName = response.display_name;
                GetComponentInParent<ProfileView>(true)?.RefreshVisual();
                TopMainView.Instance?.SetDisplayName(response.display_name);

                UIManager.Instance.ShowToast("Đổi tên thành công");
                gameObject.SetActive(false);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[UIProfileRenamePopup] player/rename failed: {ex.StatusCode} {ex.Message}");
                UIManager.Instance.ShowToast(ex.Message);
            }
            finally
            {
                isRenaming = false;
                btnConfirm.interactable = true;
            }
        }
    }
}