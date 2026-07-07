using Common;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class HitEffectObject : PoolableBehaviour
{
    [Header("Life Time")]
    [SerializeField] private float lifeTime = 0.5f;

    [SerializeField] private ParticleSystem particle;

    public override void OnSpawnedFromPool()
    {
        PlayParticles();

        if (lifeTime > 0f)
        {
            DespawnSelf(lifeTime);
        }
    }

    public override void OnDespawnedToPool()
    {
        StopParticles();
    }

    [Button]
    private void PlayParticles()
    {
        if(particle)
            particle.Play(true);
    }

    private void StopParticles()
    {
        if(particle)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}