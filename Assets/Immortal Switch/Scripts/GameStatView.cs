using Immortal_Switch.Scripts.Core;
using System;
using Battle;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStatView : MonoBehaviour
{
    [SerializeField] private TMP_Text currentDeadMonsterQuantityText;
    [SerializeField] private TMP_Text currentChapterStageText;
    [SerializeField] private Image progressSlide;
    [SerializeField] private Button buttonMap;
    [SerializeField] private Button buttonGiveUp;
    [SerializeField] private Button buttonBoss;
    [SerializeField] private GameObject shinyBossButton;
    [SerializeField] private GameObject monsterKill;
    
    [field:SerializeField]
    public BattleTimerController battleTimerController {get; private set;}
    public static GameStatView Instance { get; private set; }
    
    private const string DeadMonsterQuantityKey = "{0}/{1}";
    private const string ChapterStageKey = "{0}.{1} - Ải {2}";

    void Awake()
    {
        Instance = this;
        GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        GameEventManager.Subscribe(GameEvents.OnWaveStart, OnInitNewStage);
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Subscribe(GameEvents.OnInitNewStage,(Action<bool, bool, StageRuntimeData>) OnInitNewStage);
        
        buttonBoss.onClick.AddListener(PvEBattleController.Instance.SpawnBossDirectly);
        buttonBoss.interactable = false;
        buttonGiveUp.interactable = false;
    }

    private void OnEnemyDead(int deadCount)
    {
        currentDeadMonsterQuantityText.text = string.Format(DeadMonsterQuantityKey, deadCount, GameData.Instance.maxCreepsPerStage);
        progressSlide.fillAmount = (float)deadCount / GameData.Instance.maxCreepsPerStage;
    }

    private void OnInitNewStage()
    {
        buttonMap.gameObject.SetActive(true);
        monsterKill.SetActive(true);
        battleTimerController.HideTimer();
    }

    private void OnStageCleared(int _)
    {
        buttonBoss.interactable = false;
        battleTimerController.HideTimer();
    }

    private void OnStageLost()
    {
        buttonBoss.interactable = true;
        battleTimerController.HideTimer();
    }

    private void OnInitNewStage(bool playCompletedStage, bool isLosingStage, StageRuntimeData stageRuntimeData)
    {
        currentChapterStageText.text = string.Format(ChapterStageKey, stageRuntimeData.ChapterIndex + 1, stageRuntimeData.ChapterName, stageRuntimeData.GlobalStage);
        buttonBoss.gameObject.SetActive(!playCompletedStage);
        shinyBossButton.gameObject.SetActive(isLosingStage && !playCompletedStage);
        buttonGiveUp.gameObject.SetActive(false);
    }

    public void InitTimer(float dur, Action Act)
    {
        battleTimerController.InitTimer(dur, Act);
        buttonMap.gameObject.SetActive(false);
        monsterKill.SetActive(false);
        buttonGiveUp.gameObject.SetActive(true);
        buttonBoss.gameObject.SetActive(false);
    }
}
