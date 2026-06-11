using Immortal_Switch.Scripts.Core;
using System;
using Battle;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStatView : MonoBehaviour
{
    [SerializeField] private TMP_Text currentDeadMonsterQuantityText;
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

    void Awake()
    {
        Instance = this;
        GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        GameEventManager.Subscribe(GameEvents.OnWaveStart, OnInitNewStage);
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Subscribe(GameEvents.OnPlayCompletedStage,(Action<bool, bool>) OnPlayCompletedStage);
        
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
        currentDeadMonsterQuantityText.text = string.Format(DeadMonsterQuantityKey, 0, GameData.Instance.maxCreepsPerStage);
        progressSlide.fillAmount = 0f;
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

    private void OnPlayCompletedStage(bool playCompletedStage, bool isLosingStage)
    {
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
