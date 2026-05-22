using System;
using System.Collections.Generic;
using Battle;
using Common;
using DG.Tweening;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public enum HeroNameAction
    {
        AutoSwitchBtn,
        AutoSkillBtn,
        Skill1Btn,
        Skill2Btn,
        Skill3Btn,
        Skill4Btn,
        Skill5Btn,
        SwitchBtn,
        None,
    }

    public class CoolingData
    {
        public Dictionary<HeroNameAction, Action> callbackActs;
        public Dictionary <HeroNameAction, float> intervalCoolings;
        public Dictionary <HeroNameAction, float> timerCoolings;
        public PlayerHeroController playerHeroController;
        public List<Sprite> Sprites;
        public Sprite IconHead;
        public List<HeroNameAction> keyTimerCooling;

        public CoolingData()
        {
            callbackActs = new Dictionary<HeroNameAction, Action>();
            intervalCoolings = new Dictionary<HeroNameAction, float>();
            timerCoolings = new Dictionary<HeroNameAction, float>();
            Sprites = new List<Sprite>();
            keyTimerCooling = new List<HeroNameAction>();
        }
    }

    public class UIHeroBattleController : MonoBehaviour
    {
        public static UIHeroBattleController Instance;

        [SerializeField] UISwitchHeroController uISwitchHeroController;
        [SerializeField] TutorialController tutorialController;

        [SerializeField] Button autoSwitchBtn;
        [SerializeField] Button autoSkillBtn;
        [SerializeField] Button skill1Btn;
        [SerializeField] Button skill2Btn;
        [SerializeField] Button skill3Btn;
        [SerializeField] Button skill4Btn;
        [SerializeField] Button skill5Btn;
        [SerializeField] Button switchBtn;
        [SerializeField] Image coverSkill1;
        [SerializeField] Image coverSkill2;
        [SerializeField] Image coverSkill3;
        [SerializeField] Image coverSkill4;
        [SerializeField] Image coverSkill5;
        [SerializeField] Image coverAutoSwitch;
        [SerializeField] Image coverAutoSkill;
        [SerializeField] Image coverSwitch;
        [SerializeField] Transform autoSkillRotate;
        [SerializeField] Transform autoSwitchRotate;
        [SerializeField] List<Image> skillIcons;

        private CoolingData[] inBattleHeroesCoolingData;
        private CoolingData selectedHeroData = null;

        private Dictionary<HeroNameAction,Image> covers = null;
        private Dictionary<HeroNameAction,Image> icons = null;
        private bool isAutoSwitching = false;
        private bool isAutoSkilling = false;
        private Tween autoSkillTween;
        private Tween autoSwitchTween;

        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }

            SetStateAllCover(true);

            autoSwitchBtn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    if (!tutorialController.IsAbleActionCallback(autoSwitchBtn))
                    {
                        return;
                    }
                }

                isAutoSwitching = !isAutoSwitching;
                if (isAutoSwitching)
                {
                    autoSwitchTween?.Kill();
                    autoSwitchTween = autoSwitchRotate
                        .DOLocalRotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Incremental);
                }
                else
                {
                    autoSwitchTween?.Kill();
                    autoSwitchRotate.localRotation = Quaternion.identity;
                }
                
            });

            autoSkillBtn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    if (!tutorialController.IsAbleActionCallback(autoSkillBtn))
                    {
                        return;
                    }
                }

                isAutoSkilling = !isAutoSkilling;
                if (isAutoSkilling)
                {
                    autoSkillTween?.Kill();
                    autoSkillTween = autoSkillRotate
                        .DOLocalRotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Incremental);
                }
                else
                {
                    autoSkillTween?.Kill();
                    autoSkillRotate.localRotation = Quaternion.identity;
                }
            });

            skill1Btn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    if (!tutorialController.IsAbleActionCallback(skill1Btn))
                        return;
                }

                DoSkillAction(HeroNameAction.Skill1Btn);
            });

            skill2Btn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    return;
                }

                DoSkillAction(HeroNameAction.Skill2Btn);
            });

            skill3Btn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    return;
                }

                DoSkillAction(HeroNameAction.Skill3Btn);
            });

            skill4Btn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    return;
                }

                DoSkillAction(HeroNameAction.Skill4Btn);
            });

            skill5Btn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    return;
                }
                
                DoSkillAction(HeroNameAction.Skill5Btn);
            });

            switchBtn?.onClick.AddListener(() =>
            {
                if (tutorialController.IsIntutorial)
                {
                    if (!tutorialController.IsAbleActionCallback(switchBtn))
                    {
                        return;
                    }
                }

                DoSkillAction(HeroNameAction.SwitchBtn);
            });
        }

        private void Update()
        {
            //DoCoolingDownSkill();
        }

        public void RegisterHeroSwitch(Action<int> heroAct)
        {
            uISwitchHeroController?.RegisterActionHeroByIdx(heroAct);
        }

        private void InitUIHeroes(int index, int hid)
        {
            var currentHeroData = inBattleHeroesCoolingData[index];
            selectedHeroData = currentHeroData;
            uISwitchHeroController?.ChangeIconByIdx(0, currentHeroData.IconHead);
            if (index == 0)
            {
                AppLySpriteSkillByIdx();
            }
        }

        private void AppLySpriteSkillByIdx()
        {
            var idx = 0;
            foreach (var img in skillIcons)
            {
                img.sprite = selectedHeroData.Sprites[idx];
                idx++;
            }
        }

        public void SetStateAllCover(bool isEnable = false)
        {
            InitCovers();

            foreach (var cover in covers)
            {
                cover.Value.gameObject.SetActive(isEnable);
            }

            InitIcon();
        }

        private void InitCovers()
        {
            covers = new Dictionary<HeroNameAction, Image>()
            {
                {HeroNameAction.Skill1Btn, coverSkill1 },
                {HeroNameAction.Skill2Btn, coverSkill2 },
                {HeroNameAction.Skill3Btn, coverSkill3 },
                {HeroNameAction.Skill4Btn, coverSkill4 },
                {HeroNameAction.Skill5Btn, coverSkill5 },
                {HeroNameAction.SwitchBtn, coverSwitch },
            };
        }

        private void InitIcon()
        {
            if (icons != null && icons.Count > 0) icons.Clear();
            if (icons == null) icons = new Dictionary<HeroNameAction, Image>();
            icons = new Dictionary<HeroNameAction, Image>()
            {
                {HeroNameAction.Skill1Btn, skillIcons[0] },
                {HeroNameAction.Skill2Btn, skillIcons[1] },
                {HeroNameAction.Skill3Btn, skillIcons[2] },
                {HeroNameAction.Skill4Btn, skillIcons[3] },
                {HeroNameAction.Skill5Btn, skillIcons[4] },
                {HeroNameAction.SwitchBtn, skillIcons[5] },
            };
        }

        private void DoSkillAction(HeroNameAction hak)
        {
            if (isAutoSkilling) return;
            if (selectedHeroData == null) return;
            if (selectedHeroData.playerHeroController == null) return;
            if (selectedHeroData.playerHeroController.IsInAction()) return;
            if (!selectedHeroData.callbackActs.ContainsKey(hak)) return;

            DoSkillCallback(hak);
        }

        private bool DoAutoSkillAction(HeroNameAction hak)
        {
            if (selectedHeroData == null) return false;
            if (selectedHeroData.playerHeroController == null) return false;
            if (selectedHeroData.playerHeroController.IsInAction()) return false;
            if (!selectedHeroData.playerHeroController.IsExistTargetInRange()) return false;
            if (!selectedHeroData.callbackActs.ContainsKey(hak)) return false;
            if (hak == HeroNameAction.SwitchBtn)
            {
                if (selectedHeroData.playerHeroController.IsInFlash()) return false;
            }

            DoSkillCallback(hak);
            return true;
        }

        private bool DoAutoSkillAction(CoolingData data, HeroNameAction hak)
        {
            if (data == null) return false;
            if (data.playerHeroController == null) return false;
            if (data.playerHeroController.IsInAction()) return false;
            if (!data.playerHeroController.IsExistTargetInRange()) return false;
            if (!data.callbackActs.ContainsKey(hak)) return false;
            if (hak == HeroNameAction.SwitchBtn)
            {
                if(data.playerHeroController.IsInFlash()) return false;
            }

            ResetTimer(data, hak);
            data.callbackActs[hak]?.Invoke();
            return true;
        }

        public void SetPlayerHeroInstance(PlayerHeroController phc, int index, int hid, Dictionary<SkillSlot,int> skillIds)
        {
            var currentHeroData = inBattleHeroesCoolingData[index];
            currentHeroData.playerHeroController = phc;
            currentHeroData.IconHead = currentHeroData.playerHeroController.HeroIcon;
            uISwitchHeroController?.ChangeIconByIdx(0, currentHeroData.IconHead);
            currentHeroData.Sprites.Clear();
            currentHeroData.Sprites = new List<Sprite>()
            {
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot1]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot2]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot3]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot4]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
                // //temp
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon
            };

            InitUIHeroes(phc.HeroIndex, hid);
            uISwitchHeroController?.RegisterActionByIdx(ChangeMainHeroByIdx);
        }

        private void ChangeMainHeroByIdx(int index)
        {
            selectedHeroData = inBattleHeroesCoolingData[index];
            AppLySpriteSkillByIdx();
        }
        
        public void RegisterActionByIdx(HeroNameAction idx, Action fAct, float interval, int heroIndex)
        {
            var data = inBattleHeroesCoolingData[heroIndex];

            if(fAct != null) data.callbackActs[idx] = fAct;

            data.intervalCoolings[idx] = interval;
            data.timerCoolings.TryAdd(idx, interval);

            if(!data.keyTimerCooling.Contains(idx))
                data.keyTimerCooling.Add(idx);
        }
        
        
        public void ReplaceHeroSlot(PlayerHeroController phc, int heroId, Dictionary<SkillSlot, int> skillIds)
        {
            var data = inBattleHeroesCoolingData[phc.HeroIndex];
            data.playerHeroController = phc;

            var newSprites = new List<Sprite>()
            {
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot1]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot2]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot3]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot4]).skillIcon,
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
                // //temp
                // MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
            };

            data.IconHead = phc.HeroIcon;
            data.Sprites = newSprites;
            uISwitchHeroController?.ChangeIconByIdx(phc.HeroIndex, data.IconHead);

            AppLySpriteSkillByIdx();
        }

        private void DoSkillCallback(HeroNameAction nameAction)
        {
            SetShowCover(nameAction);
            selectedHeroData.callbackActs[nameAction]?.Invoke();
        }

        private void DoCoolingDownSkill()
        {
            for (int i = 0; i < inBattleHeroesCoolingData.Length; i++)
            {
                DoCoolingDownSkill(inBattleHeroesCoolingData[i]);
            }
        }

        private void DoCoolingDownSkill(CoolingData data)
        {
            // if (data == null) return;
            //
            // var keys = data.keyTimerCooling;
            // bool isActivedAutoSkill = false;
            // foreach (var key in keys)
            // {
            //     if (data.timerCoolings[key] > 0)
            //     {
            //         data.timerCoolings[key] -= Time.deltaTime;
            //         if (isMain == selectedHeroData.isMain)
            //         {
            //             covers[key].fillAmount = data.timerCoolings[key] / data.intervalCoolings[key];
            //         }
            //     }
            //     else
            //     {
            //         if(isMain == selectedHeroData.isMain)
            //             covers[key].gameObject.SetActive(false);
            //
            //         AutoActiveSkill(data, key);
            //         isActivedAutoSkill = true;
            //     }
            // }
            //
            // if(isActivedAutoSkill)
            // {
            //     ReOrderKeyTimerCooling(keys, data);
            // }
        }

        private void ReOrderKeyTimerCooling(List<HeroNameAction> keys, CoolingData data)
        {
            var first = keys[0];
            keys.RemoveAt(0);
            keys.Add(first);
            data.keyTimerCooling = keys;
        }

        public bool IsSkillAvailable(HeroNameAction nameAction)
        {
            return selectedHeroData.timerCoolings[nameAction] <= 0;
        }

        private void SetShowCover(HeroNameAction name)
        {
            selectedHeroData.timerCoolings[name] = selectedHeroData.intervalCoolings[name];
            covers[name].fillAmount = 1;
            covers[name].gameObject.SetActive(true);
        }

        private void ResetTimer(CoolingData data, HeroNameAction name)
        {
            data.timerCoolings[name] = data.intervalCoolings[name];
        }

        public void AutoActiveSkill(CoolingData data, HeroNameAction key)
        {
            // HeroNameAction selectedAction = key;
            // Action act = () =>
            // {
            //     if (data.isMain == selectedHeroData.isMain)
            //     {
            //         DoAutoSkillAction(selectedAction);
            //     }
            //     else
            //     {
            //         DoAutoSkillAction(data, selectedAction);
            //     }
            // };
            //
            // if (key == HeroNameAction.SwitchBtn)
            // {
            //     if (isAutoSwitching)
            //     {
            //         act.Invoke();
            //     }
            //     return;
            // }
            //
            // if (isAutoSkilling)
            // {
            //     act.Invoke();
            // }
        }
    }
}
