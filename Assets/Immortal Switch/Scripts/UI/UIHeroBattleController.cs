using Scripts.Battle;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Immortal_Switch.Scripts.UI;

namespace Scripts.UI
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

        public CoolingData(bool isMain =  false)
        {
            this.isMain = isMain;
            callbackActs = new Dictionary<HeroNameAction, Action>();
            intervalCoolings = new Dictionary<HeroNameAction, float>();
            timerCoolings = new Dictionary<HeroNameAction, float>();
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
        [SerializeField] List<Sprite> firstSprites;
        [SerializeField] List<Sprite> secondSprites;
        [SerializeField] List<Image> skillIcons;

        private CoolingData firstHeroData = new CoolingData(true);
        private CoolingData secondHeroData = new CoolingData(false);
        private CoolingData mainHeroData = null;

        private Dictionary<HeroNameAction,Image> covers = null;
        private Dictionary<HeroNameAction,Image> icons = null;
        private bool isAutoSwitching = false;
        private bool isAutoSkilling = false;
        private Sprite firstIconHead;
        private Sprite secondIconHead;
        
        private Tween autoSkillTween;
        private Tween autoSwitchTween;
        private int displayedHeroSlotIndex = 0;
        private bool IsDisplayingFirstHero => displayedHeroSlotIndex == 0;

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
                uISwitchHeroController?.ChangeIconByIdx(0, firstIconHead);
            }
            else
            {
                secondHeroData.Hid = hid;
                uISwitchHeroController?.ChangeIconByIdx(1, secondIconHead);
            }
            mainHeroData = IsDisplayingFirstHero ? firstHeroData : secondHeroData;
            RefreshDisplayedHeroUI();
        }

        private void AppLySpriteSkillByIdx(bool isFirst)
        {
            var idx = 0;
            foreach (var img in skillIcons)
            {
                if (isFirst)
                {
                    img.sprite = firstSprites[idx];
                }
                else
                {
                    img.sprite = secondSprites[idx];
                }
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
            if (mainHeroData == null) return;
            if (mainHeroData.playerHeroController == null) return;
            if (mainHeroData.playerHeroController.IsInAction()) return;
            if (!mainHeroData.intervalCoolings.ContainsKey(hak)) return;

            SetShowCover(hak);
            mainHeroData.playerHeroController.ExecuteSkill(hak);
        }

        public void SetPlayerHeroInstance(PlayerHeroController phc, int heroSlotIndex, int hid, Dictionary<SkillSlot,int> skillIds)
        {
            bool isFirstHeroSlot = heroSlotIndex == 0;

            if (isFirstHeroSlot)
            {
                firstHeroData.playerHeroController = phc;
                firstIconHead = firstHeroData.playerHeroController.HeroIcon;
                uISwitchHeroController?.ChangeIconByIdx(0, firstIconHead);
                firstSprites.Clear();
                firstSprites = new List<Sprite>()
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
                secondIconHead = secondHeroData.playerHeroController.HeroIcon;
                uISwitchHeroController?.ChangeIconByIdx(1, secondIconHead);
                secondSprites.Clear();
                secondSprites = new List<Sprite>()
                {
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot1]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot2]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot3]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot4]).skillIcon,
                    MasterDataCache.Instance.GetSkillDataById(skillIds[SkillSlot.Slot5]).skillIcon,
                    secondHeroData.playerHeroController.UISprite.SwithSkillIcon
                };
            }

            InitUIHeros(isFirstHeroSlot, hid);
            uISwitchHeroController?.RegisterActionHeroByIdx(ChangeMainHeroByIdx);
        }
        

        private void ChangeMainHeroByIdx(int hid)
        {
            displayedHeroSlotIndex = hid;
            mainHeroData = IsDisplayingFirstHero ? firstHeroData : secondHeroData;
            RefreshDisplayedHeroUI();
        }
        
        public void ToggleDisplayedHero()
        {
            displayedHeroSlotIndex = 1 - displayedHeroSlotIndex;

            RefreshDisplayedHeroUI();
        }
        
        private void RefreshDisplayedHeroUI()
        {
            mainHeroData = IsDisplayingFirstHero ? firstHeroData : secondHeroData;

            var sprites = IsDisplayingFirstHero ? firstSprites : secondSprites;

            for (int i = 0; i < skillIcons.Count; i++)
            {
                skillIcons[i].sprite = sprites[i];
            }

            foreach (var kvp in covers)
            {
                var action = kvp.Key;
                var cover = kvp.Value;

                if (mainHeroData == null ||
                    !mainHeroData.timerCoolings.ContainsKey(action) ||
                    !mainHeroData.intervalCoolings.ContainsKey(action))
                {
                    cover.gameObject.SetActive(false);
                    continue;
                }

                float timer = mainHeroData.timerCoolings[action];
                float interval = mainHeroData.intervalCoolings[action];

                if (timer > 0f && interval > 0f)
                {
                    cover.gameObject.SetActive(true);
                    cover.fillAmount = timer / interval;
                }
                else
                {
                    cover.gameObject.SetActive(false);
                }
            }
        }

        public void ChangeSkillByIdx(HeroNameAction idx, float interval, int hid)
        {
            if(firstHeroData.Hid == hid)
            {
                firstHeroData.intervalCoolings[idx] = interval;
                ChangeIconSkillBySlot((int)idx, firstSprites);
            }
            else
            {
                secondHeroData.intervalCoolings[idx] = interval;
                ChangeIconSkillBySlot((int)idx, secondSprites);
            }
        }

        private void ChangeIconSkillBySlot(int slot, List<Sprite> sprites)
        {
            sprites[slot] = null;
        }

        public void RegisterActionByIdx(HeroNameAction idx, Action fAct, float interval, bool hasCoolDown = true, bool isFirst = true)
        {
            var data = isFirst ? firstHeroData : secondHeroData;

            if (fAct != null)
                data.callbackActs[idx] = fAct;
            else if (data.callbackActs.ContainsKey(idx))
                data.callbackActs.Remove(idx);

            data.intervalCoolings[idx] = interval;

            if (hasCoolDown)
            {
                data.timerCoolings.TryAdd(idx, 0f);
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

            if (isFirstSlot)
            {
                firstIconHead = phc.HeroIcon;
                firstSprites = newSprites;
                uISwitchHeroController?.ChangeIconByIdx(0, firstIconHead);

                if (IsDisplayingFirstHero)
                {
                    mainHeroData = firstHeroData;
                    RefreshDisplayedHeroUI();
                }
            }
            else
            {
                secondIconHead = phc.HeroIcon;
                secondSprites = newSprites;
                uISwitchHeroController?.ChangeIconByIdx(1, secondIconHead);

                if (!IsDisplayingFirstHero)
                {
                    mainHeroData = secondHeroData;
                    RefreshDisplayedHeroUI();
                }
            }
        }

        private void DoSkillCallback(HeroNameAction nameAction)
        {
            if (mainHeroData == null) return;
            if (mainHeroData.playerHeroController == null) return;
            if (mainHeroData.playerHeroController.IsInAction()) return;
            if (!mainHeroData.intervalCoolings.ContainsKey(nameAction)) return;

            SetShowCover(nameAction);
            mainHeroData.playerHeroController.ExecuteSkill(nameAction);
        }

        private void DoCoolingdownSkill()
        {
            DoCoolingdownSkill(firstHeroData, IsDisplayingFirstHero);
            DoCoolingdownSkill(secondHeroData, !IsDisplayingFirstHero);
        }

        private void DoCoolingdownSkill(CoolingData data, bool isMain)
        {
            if (data == null) return;

            var keys = new List<HeroNameAction>(data.timerCoolings.Keys);

            foreach (var key in keys)
            {
                if (!data.intervalCoolings.ContainsKey(key)) continue;
                if (!covers.ContainsKey(key)) continue;

                if (data.timerCoolings[key] > 0)
                {
                    data.timerCoolings[key] -= Time.deltaTime;
                    if (isMain)
                    {
                        covers[key].fillAmount = data.timerCoolings[key] / data.intervalCoolings[key];
                    }
                }
                else
                {
                    if (isMain)
                        covers[key].gameObject.SetActive(false);
                }
            }
        }

        public bool IsSkillAvailable(HeroNameAction nameAction)
        {
            if (mainHeroData == null) return false;
            if (!mainHeroData.timerCoolings.ContainsKey(nameAction)) return false;

            return mainHeroData.timerCoolings[nameAction] <= 0;
        }

        private void SetShowCover(HeroNameAction name)
        {
            if (mainHeroData == null) return;
            if (!mainHeroData.intervalCoolings.ContainsKey(name)) return;
            if (!mainHeroData.timerCoolings.ContainsKey(name)) return;
            if (!covers.ContainsKey(name)) return;

            mainHeroData.timerCoolings[name] = mainHeroData.intervalCoolings[name];
            covers[name].fillAmount = 1;
            covers[name].gameObject.SetActive(true);
        }

        private void ResetTimer(CoolingData data, HeroNameAction name)
        {
            if (data == null) return;
            if (!data.intervalCoolings.ContainsKey(name)) return;
            if (!data.timerCoolings.ContainsKey(name)) return;

            data.timerCoolings[name] = data.intervalCoolings[name];
        }

        public void AutoActiveSkill(Func<HeroNameAction, int> endFucn = null, bool isMain = true)
        {
            HeroNameAction selectedAction = HeroNameAction.None;
            if (isAutoSkilling)
            {
                var data = isMain
                    ? mainHeroData
                    : (mainHeroData == firstHeroData ? secondHeroData : firstHeroData);

                if (data == null || data.playerHeroController == null)
                {
                    endFucn?.Invoke(selectedAction);
                    return;
                }

                foreach (var cl in data.timerCoolings)
                {
                    if (cl.Key == HeroNameAction.SwithBtn || cl.Key == HeroNameAction.AutoSwitchBtn || cl.Key == HeroNameAction.AutoSkillBtn)
                        continue;

                    if (cl.Value <= 0)
                    {
                        if (isMain)
                        {
                            DoSkillCallback(cl.Key);
                        }
                        else
                        {
                            ResetTimer(data, cl.Key);
                            data.playerHeroController.ExecuteSkill(cl.Key);
                        }

                        selectedAction = cl.Key;
                        break;
                    }
                }
            }

            endFucn?.Invoke(selectedAction);
        }

        public void AutoActiveSwitch(Action<HeroNameAction> endFucn = null, bool isMain = true)
        {
            if (!isAutoSwitching) return;

            if (isMain)
            {
                if (mainHeroData == null || mainHeroData.playerHeroController == null) return;
                if (!mainHeroData.timerCoolings.ContainsKey(HeroNameAction.SwithBtn)) return;

                if (mainHeroData.timerCoolings[HeroNameAction.SwithBtn] <= 0)
                {
                    DoSkillCallback(HeroNameAction.SwithBtn);
                    endFucn?.Invoke(HeroNameAction.SwithBtn);
                }
            }
            else
            {
                var sub = (mainHeroData == firstHeroData) ? secondHeroData : firstHeroData;
                if (sub == null || sub.playerHeroController == null) return;
                if (!sub.timerCoolings.ContainsKey(HeroNameAction.SwithBtn)) return;

                if (sub.timerCoolings[HeroNameAction.SwithBtn] <= 0)
                {
                    ResetTimer(sub, HeroNameAction.SwithBtn);
                    sub.playerHeroController.ExecuteSkill(HeroNameAction.SwithBtn);
                    endFucn?.Invoke(HeroNameAction.SwithBtn);
                }
            }
        }

        public void SwapMainHero(int hid)
        {
            displayedHeroSlotIndex = hid;
            mainHeroData = IsDisplayingFirstHero ? firstHeroData : secondHeroData;
            RefreshDisplayedHeroUI();
        }
    }
}
