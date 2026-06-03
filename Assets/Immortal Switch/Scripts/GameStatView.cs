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
        GameEventManager.Subscribe(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        
        buttonBoss.onClick.AddListener(PvEBattleController.Instance.SpawnBossDirectly);
        buttonBoss.interactable = false;
        //not available yet, so disactive
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
        buttonGiveUp.gameObject.SetActive(false);
        buttonBoss.gameObject.SetActive(true);
    }

    private void OnStageCleared()
    {
        buttonBoss.interactable = false;
        battleTimerController.HideTimer();
    }

    private void OnStageLost()
    {
        buttonBoss.interactable = true;
        battleTimerController.HideTimer();
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
