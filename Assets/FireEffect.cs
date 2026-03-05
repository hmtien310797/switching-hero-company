using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class FireEffect : MonoBehaviour
{
    public GameObject fireball;
    public GameObject fireDamageEffect;
    public GameObject fireExplosion;
    public Transform startPoint;
    public Transform endPoint;
    public float duration;
    private ParticleSystem[] particles;
    private List<Color> originalColors = new();

    private void Start()
    {
        fireball.SetActive(false);
        fireDamageEffect.SetActive(false);
        fireball.transform.position = startPoint.position;
        particles = fireDamageEffect.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            originalColors.Add(main.startColor.color);
        }
    }

    [Button]
    public void Test()
    {
        Fire();
    }

    [Button]
    public void Reset()
    {
        fireball.SetActive(false);
        fireDamageEffect.SetActive(false);
        fireball.transform.position = startPoint.position;
        fireExplosion.SetActive(false);
        ResetColor();
    }

    public async UniTask Fire()
    {
        fireball.SetActive(true);
        await fireball.transform.DOMove(endPoint.position, 0.5f).SetEase(Ease.Linear);
        fireball.SetActive(false);
        fireDamageEffect.SetActive(true);
        fireExplosion.SetActive(true);
        await FadeTo(0f);
        Reset();
    }
    

    public async UniTask FadeTo(float targetAlpha)
    {
        float time = 0;
        float startAlpha = particles[0].main.startColor.color.a;

        while (time < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SetAlpha(alpha);

            time += Time.deltaTime;
            await UniTask.Yield();
        }

        SetAlpha(targetAlpha);
    }
    
    void SetAlpha(float alpha)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;

            Color c = main.startColor.color;
            c.a = alpha;

            main.startColor = c;
        }
    }

    public void ResetColor()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            main.startColor = originalColors[i];
        }
    }
}
