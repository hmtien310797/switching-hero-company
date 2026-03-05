using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

public class SkillDemoController : MonoBehaviour
{
    public GameObject waterParticle;
    public GameObject waterSplashParticle;
    public FireEffect fireEffect;
    public FireEffect fireEffect1;
    public FireEffect fireEffect2;
    
    [Button]
    public void PlayWaterSplash()
    {
        waterSplashParticle.SetActive(true);
    }

    [Button]
    public void PlayCombo()
    {
        FireComboAsync();
    }
    
    [Button]
    public void PlayWaterParticle()
    {
        waterParticle.SetActive(true);
    }

    public async UniTask FireComboAsync()
    {
        fireEffect.Fire();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        fireEffect1.Fire();  
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        fireEffect2.Fire();
    }
}
