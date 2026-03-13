using Scripts.Battle;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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
        private PlayerHeroController playerHeroController;
        
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
                DoSkillAction(HeroNameAction.Skill1Btn);
            });

            skill2Btn?.onClick.AddListener(() =>
            {
                DoSkillAction(HeroNameAction.Skill2Btn);
            });

            skill3Btn?.onClick.AddListener(() =>
            {
                DoSkillAction(HeroNameAction.Skill3Btn);
            });

            skill4Btn?.onClick.AddListener(() =>
            {
                DoSkillAction(HeroNameAction.Skill4Btn);
            });

            skill5Btn?.onClick.AddListener(() =>
            {
                DoSkillAction(HeroNameAction.Skill5Btn);
            });

            switchBtn?.onClick.AddListener(() =>
            {
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
            }
            else
            {
                secondHeroData.Hid = hid;
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
            if (isAutoSkilling || playerHeroController.IsInAction()) return;

            SetShowCover(hak);
            mainHeroData.callbackActs[hak]?.Invoke();
        }

        public void SetPlayerHeroInstance(PlayerHeroController phc, bool isMain, int hid)
        {
            playerHeroController = phc;
            InitUIHeros(isMain, hid);
            uISwitchHeroController?.RegisterActionByIdx(ChangeMainHeroByIdx);
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

        public void RegisterActionByIdx(HeroNameAction idx, Action fAct, float interval, bool hasCoolDown = true , bool isFirst = true)
        {
            if(isFirst)
            {
                if (!firstHeroData.callbackActs.ContainsKey(idx))
                {
                    firstHeroData.callbackActs[idx] = fAct;
                }

                if (!firstHeroData.intervalCoolings.ContainsKey(idx))
                {
                    firstHeroData.intervalCoolings.Add(idx, interval);
                }

                if (!firstHeroData.timerCoolings.ContainsKey(idx) && hasCoolDown)
                {
                    firstHeroData.timerCoolings.Add(idx, interval);
                }
            }
            else
            {
                if (!secondHeroData.callbackActs.ContainsKey(idx))
                {
                    secondHeroData.callbackActs[idx] = fAct;
                }

                if (!secondHeroData.intervalCoolings.ContainsKey(idx))
                {
                    secondHeroData.intervalCoolings.Add(idx, interval);
                }

                if (!secondHeroData.timerCoolings.ContainsKey(idx) && hasCoolDown)
                {
                    secondHeroData.timerCoolings.Add(idx, interval);
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
                    if(isMain)
                        covers[key].fillAmount = data.timerCoolings[key] / data.intervalCoolings[key];
                }
                else
                {
                    if(isMain)
                        covers[key].gameObject.SetActive(false);
                }
            }
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
