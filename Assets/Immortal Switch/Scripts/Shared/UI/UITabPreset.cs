using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public enum ETabPresetStatus
    {
        Normal = 0,
        Selected = 1,
        Lock = 2,
        Disabled = 3
    }

    [RequireComponent(typeof(Button))]
    public class UITabPreset : MonoBehaviour
    {
        [Header("Main references")]
        [SerializeField]
        private Button btn;

        [SerializeField]
        private bool keepInteractableOnHover;

        [Header("Selected references")]
        [SerializeField]
        private TextMeshProUGUI txtSelected;

        [SerializeField]
        private GameObject goSelected;

        [Header("Normal references")]
        [SerializeField]
        private TextMeshProUGUI txtNormal;

        [SerializeField]
        private GameObject goNormal;

        [Header("Status references")]
        [SerializeField]
        private GameObject goLock;

        // --- Private Fields
        // index cua tab
        private int _idx;

        // măc định -= null, set runtime = code
        private ETabPresetStatus? _status;

        // action cua tab preset hien tai
        private Action<int> _onClick;

        private void Awake()
        {
            btn.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            _onClick?.Invoke(_idx);
        }

        public void Bind(int idx, string label, Action<int> onClick)
        {
            _idx = idx;
            _onClick = onClick;

            SetLabel(label);
        }

        public void SetLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            if (txtSelected != null)
            {
                txtSelected.text = label;
            }

            if (txtNormal != null)
            {
                txtNormal.text = label;
            }
        }

        public void SetInteractable(bool value)
        {
            btn.interactable = value;
        }

        public virtual void SetStatus(ETabPresetStatus status)
        {
            // trang thai cu bo qua
            if (_status == status)
            {
                return;
            }

            _status = status;

            switch (status)
            {
                case ETabPresetStatus.Normal:
                    gameObject.SetActive(true);
                    RefreshNormal(true);
                    RefreshSelected(false);
                    RefreshVisual();
                    break;

                case ETabPresetStatus.Selected:
                    gameObject.SetActive(true);
                    RefreshNormal(false);
                    RefreshSelected(true);
                    RefreshVisual();
                    break;

                case ETabPresetStatus.Lock:
                    gameObject.SetActive(true);
                    RefreshNormal(false);
                    RefreshSelected(false);
                    RefreshVisual();
                    break;
                
                case ETabPresetStatus.Disabled:
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void RefreshNormal(bool value)
        {
            if (goNormal != null)
            {
                goNormal.SetActive(value);
            }
        }

        private void RefreshSelected(bool value)
        {
            if (goSelected != null)
            {
                goSelected.SetActive(value);
            }
        }

        private void RefreshVisual()
        {
            var isLock = _status == ETabPresetStatus.Lock;

            if (goLock != null)
            {
                goLock.SetActive(isLock);
            }

            if (!keepInteractableOnHover)
            {
                SetInteractable(_status == ETabPresetStatus.Normal);
            }
        }
    }
}