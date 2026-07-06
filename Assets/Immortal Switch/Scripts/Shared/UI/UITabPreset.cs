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
    }

    [RequireComponent(typeof(Button))]
    public class UITabPreset : MonoBehaviour
    {
        [Header("Main references")] [SerializeField]
        private Button btn;

        [Header("Selected references")] [SerializeField]
        private TextMeshProUGUI txtSelected;

        [SerializeField] private GameObject goSelected;

        [Header("Normal references")] [SerializeField]
        private TextMeshProUGUI txtNormal;

        [SerializeField] private GameObject goNormal;

        [Header("Status references")] [SerializeField]
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

        public void Bind(int idx, string txt, Action<int> onClick)
        {
            _idx = idx;
            _onClick = onClick;

            SetLabel(txt);
        }

        public void SetLabel(string txt)
        {
            if (txtSelected != null)
            {
                txtSelected.text = txt;
            }

            if (txtNormal != null)
            {
                txtNormal.text = txt;
            }
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
                    RefreshNormal(true);
                    RefreshSelected(false);
                    RefreshVisual();
                    break;

                case ETabPresetStatus.Selected:
                    RefreshNormal(false);
                    RefreshSelected(true);
                    RefreshVisual();
                    break;

                case ETabPresetStatus.Lock:
                    RefreshNormal(false);
                    RefreshSelected(false);
                    RefreshVisual();
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

            btn.interactable = _status == ETabPresetStatus.Normal;
        }
    }
}