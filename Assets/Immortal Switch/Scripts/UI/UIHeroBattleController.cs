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

                SetShowCover(HeroNameAction.SwithBtn);
                mainHeroData.callbackActs[HeroNameAction.SwithBtn]?.Invoke();
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
                mainHeroData = firstHeroData;
                AppLySpriteSkillByIdx(true);
                uISwitchHeroController?.ChangeIconByIdx(0, firstIconHead);
            }
            else
            {
                secondHeroData.Hid = hid;
                uISwitchHeroController?.ChangeIconByIdx(1, secondIconHead);
            }
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
            if (!mainHeroData.callbackActs.ContainsKey(hak)) return;

            SetShowCover(hak);
            mainHeroData.callbackActs[hak]?.Invoke();
            
        }

        public void SetPlayerHeroInstance(PlayerHeroController phc, bool isMain, int hid, Dictionary<SkillSlot,int> skillIds)
        {
            if (isMain)
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
            InitUIHeros(isMain, hid);
            uISwitchHeroController?.RegisterActionByIdx(ChangeMainHeroByIdx);
        }

        private void SetHeadIconById(int hid)
        {

        }

        private void ChangeMainHeroByIdx(int hid)
        {
            if(hid == 0)
            {
                mainHeroData = firstHeroData;
                firstHeroData.isMain = true;
                secondHeroData.isMain = false;
                AppLySpriteSkillByIdx(true);
            }
            else
            {
                mainHeroData = secondHeroData;
                firstHeroData.isMain = false;
                secondHeroData.isMain = true;
                AppLySpriteSkillByIdx(false);
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

            data.callbackActs[idx] = fAct;
            data.intervalCoolings[idx] = interval;

            if (hasCoolDown)
            {
                if (!data.timerCoolings.ContainsKey(idx))
                    data.timerCoolings[idx] = interval;
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

                if (firstHeroData.isMain)
                {
                    mainHeroData = firstHeroData;
                    AppLySpriteSkillByIdx(true);
                }
            }
            else
            {
                secondIconHead = phc.HeroIcon;
                secondSprites = newSprites;
                uISwitchHeroController?.ChangeIconByIdx(1, secondIconHead);

                if (secondHeroData.isMain)
                {
                    mainHeroData = secondHeroData;
                    AppLySpriteSkillByIdx(false);
                }
            }
        }

        private void DoSkillCallback(HeroNameAction nameAction)
        {
            SetShowCover(nameAction);
            mainHeroData.callbackActs[nameAction]?.Invoke();
        }

        private void DoCoolingdownSkill()
        {
            DoCoolingdownSkill(firstHeroData, firstHeroData.isMain);
            DoCoolingdownSkill(secondHeroData, secondHeroData.isMain);
        }

        private void DoCoolingdownSkill(CoolingData data, bool isMain)
        {
            if (data == null) return;

            var keys = new List<HeroNameAction>(data.timerCoolings.Keys);

            foreach (var key in keys)
            {
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
                    if(isMain)
                        covers[key].gameObject.SetActive(false);
                }
            }
        }

        public bool IsSkillAvailable(HeroNameAction nameAction)
        {
            return mainHeroData.timerCoolings[nameAction] <= 0;
        }

        private void SetShowCover(HeroNameAction name)
        {
            mainHeroData.timerCoolings[name] = mainHeroData.intervalCoolings[name];
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
                var data = isMain ? mainHeroData : firstHeroData.isMain ? secondHeroData : firstHeroData;
                foreach (var cl in data.timerCoolings)
                {
                    if(cl.Key == HeroNameAction.SwithBtn || cl.Key == HeroNameAction.AutoSwitchBtn || cl.Key == HeroNameAction.AutoSkillBtn) continue;

                    if (cl.Value <= 0)
                    {
                        if (isMain)
                        {
                            DoSkillCallback(cl.Key);
                        }
                        else
                        {
                            ResetTimer(data, cl.Key);
                            data.callbackActs[cl.Key]?.Invoke();
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
            if(isAutoSwitching)
            {
                if (isMain)
                {
                    if (mainHeroData.timerCoolings[HeroNameAction.SwithBtn] <= 0)
                    {
                        DoSkillCallback(HeroNameAction.SwithBtn);

                        endFucn?.Invoke(HeroNameAction.SwithBtn);
                    }
                }
                else
                {
                    var sub = (mainHeroData.Hid == firstHeroData.Hid) ? secondHeroData : firstHeroData;
                    if (sub.timerCoolings[HeroNameAction.SwithBtn] <= 0)
                    {
                        ResetTimer(sub, HeroNameAction.SwithBtn);
                        sub.callbackActs[HeroNameAction.SwithBtn]?.Invoke();
                        endFucn?.Invoke(HeroNameAction.SwithBtn);
                    }
                }
            }
        }

        public void SwapMainHero(int hid)
        {
            if(firstHeroData.isMain)
            {
                firstHeroData.isMain = false;
                secondHeroData.isMain = true;
                mainHeroData = secondHeroData;
            }
            else
            {
                secondHeroData.isMain = false;
                firstHeroData.isMain = true;
                mainHeroData = firstHeroData;
            }
        }
    }
}
