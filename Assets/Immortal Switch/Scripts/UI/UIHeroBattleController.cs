using Scripts.Battle;
using System;
using System.Collections.Generic;
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

    public class UIHeroBattleController : MonoBehaviour
    {
        public static UIHeroBattleController Instance;

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

        private Dictionary<HeroNameAction, Action> actions = new Dictionary<HeroNameAction, Action>();
        private Dictionary<HeroNameAction, float> intervalCoolings = new Dictionary<HeroNameAction, float>();
        private Dictionary<HeroNameAction, float> coolingsTime = new Dictionary<HeroNameAction, float>();
        private Dictionary<HeroNameAction,Image> covers = null;
        private bool isAutoSwitching = false;
        private bool isAutoSkilling = false;
        private PlayerHeroController playerHeroController;

        public void SetStateAllCover(bool isEnable = false)
        {
            InitCovers();

            foreach (var cover in covers)
            {
                cover.Value.gameObject.SetActive(isEnable);
            }
        }

        private void InitCovers()
        {
            covers = new Dictionary<HeroNameAction, Image>()
            {
                {HeroNameAction.AutoSwitchBtn, coverAutoSwitch },
                {HeroNameAction.AutoSkillBtn, coverAutoSkill },
                {HeroNameAction.Skill1Btn, coverSkill1 },
                {HeroNameAction.Skill2Btn, coverSkill2 },
                {HeroNameAction.Skill3Btn, coverSkill3 },
                {HeroNameAction.Skill4Btn, coverSkill4 },
                {HeroNameAction.Skill5Btn, coverSkill5 },
                {HeroNameAction.SwithBtn, coverSwitch },
            };
        }

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
                SetShowCover(HeroNameAction.AutoSwitchBtn);
                actions[HeroNameAction.AutoSwitchBtn]?.Invoke();
            });

            autoSkillBtn?.onClick.AddListener(() =>
            {
                isAutoSkilling = !isAutoSkilling;
                SetShowCover(HeroNameAction.AutoSkillBtn);
                actions[HeroNameAction.AutoSkillBtn]?.Invoke();
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
                actions[HeroNameAction.SwithBtn]?.Invoke();
            });
        }

        private void Update()
        {
            DoCoolingdownSkill();
        }

        private void DoSkillAction(HeroNameAction hak)
        {
            if (isAutoSkilling || playerHeroController.IsInAction()) return;

            SetShowCover(hak);
            actions[hak]?.Invoke();
        }

        public void SetPlayerHeroInstance(PlayerHeroController phc)
        {
            playerHeroController = phc;
        }

        public void RegisterActionByIdx(HeroNameAction idx, Action fAct, float interval)
        {
            if (!actions.ContainsKey(idx))
            {
                actions[idx] = fAct;
            }

            if (!intervalCoolings.ContainsKey(idx))
            {
                intervalCoolings.Add(idx, interval);
            }

            if(!coolingsTime.ContainsKey(idx))
            {
                coolingsTime.Add(idx, interval);
            }
        }

        private void DoSkillCallback(HeroNameAction nameAction)
        {
            SetShowCover(nameAction);
            actions[nameAction]?.Invoke();
        }

        private void DoCoolingdownSkill()
        {
            if(coolingsTime == null || coolingsTime.Count == 0) return;

            var keys = new List<HeroNameAction>(coolingsTime.Keys);

            foreach (var key in keys)
            {
                if (coolingsTime[key] > 0)
                {
                    coolingsTime[key] -= Time.deltaTime;

                    //if (key == HeroNameAction.SwithBtn) continue;

                    covers[key].fillAmount = coolingsTime[key] / intervalCoolings[key];
                }
                else
                {
                    covers[key].gameObject.SetActive(false);
                }
            }
        }

        private void SetShowCover(HeroNameAction name)
        {
            /*if(name != HeroNameAction.SwithBtn)
            {
                coolingsTime[name] = intervalCoolings[name];
                covers[name].fillAmount = 1;
            }
            else
            {
                coolingsTime[name] = intervalCoolings[name];
            }*/
            coolingsTime[name] = intervalCoolings[name];
            covers[name].fillAmount = 1;
            covers[name].gameObject.SetActive(true);
        }

        public void AutoActiveSkill(Func<HeroNameAction, int> endFucn = null)
        {
            HeroNameAction selectedAction = HeroNameAction.None;
            if (isAutoSkilling)
            {
                foreach (var cl in coolingsTime)
                {
                    if(cl.Key == HeroNameAction.SwithBtn || cl.Key == HeroNameAction.AutoSwitchBtn || cl.Key == HeroNameAction.AutoSkillBtn) continue;

                    if (cl.Value <= 0)
                    {
                        DoSkillCallback(cl.Key);
                        selectedAction = cl.Key;
                        break;
                    }
                }
            }
            endFucn?.Invoke(selectedAction);
        }

        public void AutoActiveSwitch(Action<HeroNameAction> endFucn = null)
        {
            if(isAutoSwitching)
            {
                if (coolingsTime[HeroNameAction.SwithBtn] <= 0)
                {
                    DoSkillCallback(HeroNameAction.SwithBtn);

                    endFucn?.Invoke(HeroNameAction.SwithBtn);
                }
            }
        }
    }
}
