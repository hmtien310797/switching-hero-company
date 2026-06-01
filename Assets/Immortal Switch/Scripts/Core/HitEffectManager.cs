using Common;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

public sealed class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private HitEffectObject hitEffectPrefab;

    [Header("Spawn")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Play(ICombatUnit combatTarget, Transform target = null)
    {
        if (hitEffectPrefab == null)
            return;

        Vector3 spawnPosition = combatTarget.Position + offset;

        PoolManager.Instance.Spawn(
            hitEffectPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}