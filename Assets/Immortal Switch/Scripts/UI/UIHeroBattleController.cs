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
        SwithBtn,
        None,
    }

    public class CoolingData
    {
        public int Hid;
        public bool isMain;
        public Dictionary<HeroNameAction, Action> callbackActs;
        public Dictionary <HeroNameAction, float> intervalCoolings;
        public Dictionary <HeroNameAction, float> timerCoolings;
        public PlayerHeroController playerHeroController;
        public List<Sprite> Sprites;
        public Sprite IconHead;
        public List<HeroNameAction> keyTimerCooling;

        public CoolingData(bool isMain =  false)
        {
            this.isMain = isMain;
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

        private CoolingData firstHeroData = new CoolingData(true);
        private CoolingData secondHeroData = new CoolingData(false);
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

                DoSkillAction(HeroNameAction.SwithBtn);
            });
        }

        private void Update()
        {
            DoCoolingdownSkill();
        }

        public void RegisterHeroSwitch(Action<int> heroAct)
        {
            uISwitchHeroController?.RegisterActionHeroByIdx(heroAct);
        }

        private void InitUIHeros(bool ismain, int hid)
        {
            if (ismain)
            {
                firstHeroData.Hid = hid;
                selectedHeroData = firstHeroData;
                AppLySpriteSkillByIdx();
                uISwitchHeroController?.ChangeIconByIdx(0, firstHeroData.IconHead);
            }
            else
            {
                secondHeroData.Hid = hid;
                uISwitchHeroController?.ChangeIconByIdx(1, secondHeroData.IconHead);
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
                {HeroNameAction.SwithBtn, coverSwitch },
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
                {HeroNameAction.SwithBtn, skillIcons[5] },
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

        private void DoAutoSkillAction(HeroNameAction hak, out bool isSuccessed)
        {
            isSuccessed = false;
            if (selectedHeroData == null) return;
            if (selectedHeroData.playerHeroController == null) return;
            if (selectedHeroData.playerHeroController.IsInAction()) return;
            if (!selectedHeroData.playerHeroController.IsExistTargetInRange()) return;
            if (!selectedHeroData.callbackActs.ContainsKey(hak)) return;
            if (hak == HeroNameAction.SwithBtn)
            {
                if (selectedHeroData.playerHeroController.IsInFlash()) return;
            }

            DoSkillCallback(hak);
            isSuccessed = true;
        }

        private void DoAutoSkillAction(CoolingData data, HeroNameAction hak, out bool isSuccesed)
        {
            isSuccesed = false;
            if (data == null) return;
            if (data.playerHeroController == null) return;
            if (data.playerHeroController.IsInAction()) return;
            if (!data.playerHeroController.IsExistTargetInRange()) return;
            if (!data.callbackActs.ContainsKey(hak)) return;
            if(hak == HeroNameAction.SwithBtn)
            {
                if(data.playerHeroController.IsInFlash()) return;
            }

            ResetTimer(data, hak);
            data.callbackActs[hak]?.Invoke();
            isSuccesed = true;
        }

        public void SetPlayerHeroInstance(PlayerHeroController phc, bool isMain, int hid, Dictionary<SkillSlot,int> skillIds)
        {
            if (isMain)
            {
                firstHeroData.playerHeroController = phc;
                firstHeroData.IconHead = firstHeroData.playerHeroController.HeroIcon;
                uISwitchHeroController?.ChangeIconByIdx(0, firstHeroData.IconHead);
                firstHeroData.Sprites.Clear();
                firstHeroData.Sprites = new List<Sprite>()
                {
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot1]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot2]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot3]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot4]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
                    firstHeroData.playerHeroController.UISprite.SwithSkillIcon
                };
            }
            else
            {
                secondHeroData.playerHeroController = phc;
                secondHeroData.IconHead = secondHeroData.playerHeroController.HeroIcon;
                uISwitchHeroController?.ChangeIconByIdx(1, secondHeroData.IconHead);
                secondHeroData.Sprites.Clear();
                secondHeroData.Sprites = new List<Sprite>()
                {
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot1]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot2]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot3]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot4]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
                    secondHeroData.playerHeroController.UISprite.SwithSkillIcon
                };
            }

            InitUIHeros(isMain, hid);
            uISwitchHeroController?.RegisterActionByIdx(ChangeMainHeroByIdx);
        }

        private void ChangeMainHeroByIdx(int hid)
        {
            if(hid == 0)
            {
                selectedHeroData = firstHeroData;
            }
            else
            {
                selectedHeroData = secondHeroData;
            }

            AppLySpriteSkillByIdx();
        }

        public void ChangeSkillByIdx(HeroNameAction idx, float interval, int hid)
        {
            if(firstHeroData.Hid == hid)
            {
                firstHeroData.intervalCoolings[idx] = interval;
                ChangeIconSkillBySlot((int)idx, firstHeroData.Sprites);
            }
            else
            {
                secondHeroData.intervalCoolings[idx] = interval;
                ChangeIconSkillBySlot((int)idx, secondHeroData.Sprites);
            }
        }

        private void ChangeIconSkillBySlot(int slot, List<Sprite> sprites)
        {
            sprites[slot] = null;
        }

        public void RegisterActionByIdx(HeroNameAction idx, Action fAct, float interval, bool hasCoolDown = true, bool isFirst = true)
        {
            var data = isFirst ? firstHeroData : secondHeroData;

            if(fAct != null) data.callbackActs[idx] = fAct;

            if (hasCoolDown)
            {
                data.intervalCoolings[idx] = interval;

                if (!data.timerCoolings.ContainsKey(idx))
                    data.timerCoolings[idx] = interval;

                if(!data.keyTimerCooling.Contains(idx))
                    data.keyTimerCooling.Add(idx);
            }
            else
            {
                if (data.timerCoolings.ContainsKey(idx))
                    data.timerCoolings.Remove(idx);
            }
        }
        
        private void ResetHeroSlotData(CoolingData data)
        {
            bool keepIsMain = data.isMain;

            data.callbackActs.Clear();
            data.intervalCoolings.Clear();
            data.timerCoolings.Clear();
            data.playerHeroController = null;
            data.Hid = 0;
            data.isMain = keepIsMain;
        }
        
        public void ReplaceHeroSlot(PlayerHeroController phc, bool isFirstSlot, int hid, Dictionary<SkillSlot, int> skillIds)
        {
            var data = isFirstSlot ? firstHeroData : secondHeroData;
            //ResetHeroSlotData(data);

            data.Hid = hid;
            data.playerHeroController = phc;

            var newSprites = new List<Sprite>()
            {
                MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot1]).skillIcon,
                MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot2]).skillIcon,
                MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot3]).skillIcon,
                MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot4]).skillIcon,
                MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
                phc.UISprite.SwithSkillIcon
            };

            data.IconHead = phc.HeroIcon;
            data.Sprites = newSprites;
            if (isFirstSlot)
            {
                uISwitchHeroController?.ChangeIconByIdx(0, data.IconHead);
            }
            else
            {
                uISwitchHeroController?.ChangeIconByIdx(1, data.IconHead);
            }

            AppLySpriteSkillByIdx();
        }

        private void DoSkillCallback(HeroNameAction nameAction)
        {
            SetShowCover(nameAction);
            selectedHeroData.callbackActs[nameAction]?.Invoke();
        }

        private void DoCoolingdownSkill()
        {
            DoCoolingdownSkill(firstHeroData, firstHeroData.isMain);
            DoCoolingdownSkill(secondHeroData, secondHeroData.isMain);
        }

        private void DoCoolingdownSkill(CoolingData data, bool isMain)
        {
            if (data == null) return;

            var keys = data.keyTimerCooling;
            bool isActivedAutoSkill = false;
            foreach (var key in keys)
            {
                if (data.timerCoolings[key] > 0)
                {
                    data.timerCoolings[key] -= Time.deltaTime;
                    if (isMain == selectedHeroData.isMain)
                    {
                        covers[key].fillAmount = data.timerCoolings[key] / data.intervalCoolings[key];
                    }
                }
                else
                {
                    if(isMain == selectedHeroData.isMain)
                        covers[key].gameObject.SetActive(false);

                    AutoActiveSkill(data, key);
                    isActivedAutoSkill = true;
                }
            }

            if(isActivedAutoSkill)
            {
                ReOrderKeyTimerCooling(keys, data);
            }
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

        public void AutoActiveSkill(Func<HeroNameAction, int> endFucn = null, bool isMain = true)
        {
            HeroNameAction selectedAction = HeroNameAction.None;
            if (isAutoSkilling)
            {
                var data = !isMain ? secondHeroData : firstHeroData;
                foreach (var cl in data.timerCoolings)
                {
                    selectedAction = cl.Key;
                    if (selectedAction == HeroNameAction.SwithBtn || selectedAction == HeroNameAction.AutoSwitchBtn || selectedAction == HeroNameAction.AutoSkillBtn) continue;

                    if (cl.Value <= 0)
                    {
                        var isSuccessed = false;
                        if (data.isMain == selectedHeroData.isMain)
                        {
                            DoAutoSkillAction(selectedAction, out isSuccessed);
                        }
                        else
                        {
                            DoAutoSkillAction(data, selectedAction, out isSuccessed);
                        }
                        
                        if(isSuccessed) break;
                    }
                }
            }
            endFucn?.Invoke(selectedAction);
        }

        public void AutoActiveSkill(CoolingData data, HeroNameAction key)
        {
            HeroNameAction selectedAction = key;
            Action act = () =>
            {
                var isSuccessed = false;
                if (data.isMain == selectedHeroData.isMain)
                {
                    DoAutoSkillAction(selectedAction, out isSuccessed);
                }
                else
                {
                    DoAutoSkillAction(data, selectedAction, out isSuccessed);
                }
            };

            if (key == HeroNameAction.SwithBtn)
            {
                if (isAutoSwitching)
                {
                    act.Invoke();
                }
                return;
            }

            if (isAutoSkilling)
            {
                act.Invoke();
            }
        }

        public void AutoActiveSwitch(Action<HeroNameAction> endFucn = null, bool isMain = true)
        {
            if(isAutoSwitching)
            {
                var data = !isMain ? secondHeroData : firstHeroData;
                if (data.timerCoolings[HeroNameAction.SwithBtn] <= 0)
                {
                    var isSuccessed = false;
                    if (data.isMain == selectedHeroData.isMain)
                    {
                        DoAutoSkillAction(HeroNameAction.SwithBtn, out isSuccessed);
                    }
                    else
                    {
                        DoAutoSkillAction(data, HeroNameAction.SwithBtn, out isSuccessed);
                    }
                }
            }

            endFucn?.Invoke(HeroNameAction.SwithBtn);
        }

        public void SwapMainHero(int hid)
        {
            if(firstHeroData.isMain)
            {
                firstHeroData.isMain = false;
                secondHeroData.isMain = true;
                selectedHeroData = secondHeroData;
            }
            else
            {
                secondHeroData.isMain = false;
                firstHeroData.isMain = true;
                selectedHeroData = firstHeroData;
            }
        }
    }
}
