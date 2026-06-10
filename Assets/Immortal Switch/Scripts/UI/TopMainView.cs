using System;
using Battle;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI.Skill;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.PlayerSystem.UI;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.StageSelection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class TopMainView : UIView
    {
        public static TopMainView Instance;

        [SerializeField] BattleResultController battleResultController;
        [SerializeField] BattleTimerController battleTimerController;
        [SerializeField] HeroSkillBarUI heroSkillBarUI;
        [SerializeField] private Button switchMainSubHeroButton;
        [SerializeField] private Button moveButton;
        [SerializeField] CurrencyView currencyView;
        [SerializeField] HeroJoystick heroJostick;
        [SerializeField] private Button autoSkillButton;
        [SerializeField] private Button profileBtn;
        [SerializeField] private GameObject rotateObject;
        [SerializeField] private GameObject[] hideAbleObjects;
        
        public HeroSkillBarUI HeroSkillBarUI => heroSkillBarUI;
        private bool isAutoActived = false;

        private void Awake()
        {
            Instance = this;

            if (switchMainSubHeroButton != null)
                switchMainSubHeroButton.onClick.AddListener(OnSwitchMainSubHeroButtonClicked);

            //profileBtn.onClick.AddListener(OnClickProfile);
            HideAbleObjects();
        }

        private void OnClickProfile()
        {
            UIManager.Instance.OpenPopupAsync<UIProfileView>();
        }

        [Button]
        private void OpenIdleFarmingScreen()
        {
            StageRuntimeData stageRuntimeData = PvEBattleController.Instance.GetStageRuntimeData();
            FarmingIdleScreenService.Open(stageRuntimeData);
        }
        
        [Button]
        private void CloseIdleFarmingScreen()
        {
            FarmingIdleScreenService.Close();
        }

        private void Start()
        {
            autoSkillButton.onClick.AddListener(()=>
            {
                isAutoActived = !isAutoActived;
                PvEBattleController.Instance.SetAutoSkill(isAutoActived);
                rotateObject.transform.DOLocalRotate(new Vector3(0, 0, isAutoActived ? 180 : 0), 0.2f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
            });

            SetHeroTeamController(HeroTeamController.Instance);
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageEnd);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
            GameEventManager.Subscribe(GameEvents.OnWaveStart, OnStageStart);
            moveButton.onClick.AddListener(() =>
            {
                UIManager.Instance.TogglePopupAsync<StageSelectionView>(new StageSelectionOpenArgs
                {
                    CurrentStage = PvEBattleController.Instance.CurrentStage,
                    HighestUnlockedStage = PvEBattleController.Instance.HighestUnlockedStage
                }).Forget();
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (switchMainSubHeroButton != null)
                switchMainSubHeroButton.onClick.RemoveListener(OnSwitchMainSubHeroButtonClicked);
        }

        private void OnSwitchMainSubHeroButtonClicked()
        {
            PvEBattleController.Instance?.OnSwitchMainSubHeroButtonClicked();
        }

        private void SetHeroTeamController(HeroTeamController heroTeamController)
        {
            heroJostick.SetTarget(heroTeamController);
        }

        public CurrencyView CurrencyView => currencyView;

        private void OnStageLost()
        {
            HideAbleObjects();
        }
        
        private void OnStageEnd(int _)
        {
            HideAbleObjects();
        }

        private void HideAbleObjects()
        {
            for (int i = 0; i < hideAbleObjects.Length; i++)
            {
                hideAbleObjects[i].SetActive(false);
            }
        }
        
        private void OnStageStart()
        {
            for (int i = 0; i < hideAbleObjects.Length; i++)
            {
                hideAbleObjects[i].SetActive(true);
            }
        }
    }
}
