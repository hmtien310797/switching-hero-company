using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class UISwitchHeroController : MonoBehaviour
    {
        [SerializeField] Button heroBtnA;
        [SerializeField] Button heroBtnB;
        [SerializeField] List<GameObject> covers;
        [SerializeField] List<Image> icons;

        private int selectedBtnIdx = -1;
        private Action<int> selectedAct = null;
        private Action<int> heroAction = null;

        void Start()
        {
            heroBtnA?.onClick.AddListener(() =>
            {
                if (selectedBtnIdx == 0) return;
                ActiveHeroBtn(0);
            });

            heroBtnB?.onClick.AddListener(() => 
            {
                if (selectedBtnIdx == 1) return;
                ActiveHeroBtn(1);
            });

            ActiveHeroBtn(0, true);
        }

        public void RegisterActionByIdx(Action<int> fAct)
        {
            selectedAct = fAct;
        }

        public void RegisterActionHeroByIdx(Action<int> fAct)
        {
            heroAction += fAct;
        }

        public void ActiveHeroBtn(int idx, bool isInit = false)
        {
            if ((idx >= covers.Count))
            {
                return;
            }

            selectedBtnIdx = idx;
            covers?[selectedBtnIdx].SetActive(true);
            var unSelectedIdx = selectedBtnIdx == 0 ? 1 : 0;
            covers?[unSelectedIdx].SetActive(false);
            selectedAct?.Invoke(idx);
            if(!isInit)
                heroAction?.Invoke(idx);
        }

        private void ChangeIconByIdx(int idx, Sprite sprite)
        {
            if (idx >= icons.Count) return;

            var oldSprite = icons[idx].sprite;
            icons[idx].sprite = sprite;

            if(oldSprite != null && oldSprite.texture != null)
            {
                Destroy(oldSprite.texture);
                Destroy(oldSprite);
            }
        }

    }
}
