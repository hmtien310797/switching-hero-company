using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.DamageNumber
{
    public class DamageNumberService : MonoBehaviour
    {
        public static DamageNumberService Instance { get; private set; }

        [Header("Damage Number Pro Prefabs")]
        [SerializeField] private DamageNumbersPro.DamageNumber normalPrefab;
        [SerializeField] private DamageNumbersPro.DamageNumber critPrefab;
        [SerializeField] private DamageNumbersPro.DamageNumber healPrefab;
        [SerializeField] private DamageNumbersPro.DamageNumber missPrefab;

        [Header("Spawn")]
        [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private Vector3 randomOffset = new Vector3(0.35f, 0.15f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ShowDamage(float value, Vector3 position, DamageType type)
        {
            if (value <= 1)
            {
                return;
            }
            DamageNumbersPro.DamageNumber prefab = GetPrefab(type);

            if (prefab == null)
            {
                Debug.LogWarning($"[DamageNumberService] Missing prefab for {type}");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(position);

            if (type == DamageType.Miss)
            {
                prefab.Spawn(spawnPosition, "MISS");
            }
            else if (type == DamageType.Heal)
            {
                prefab.Spawn(spawnPosition, $"+{Mathf.RoundToInt(value)}");
            }
            else
            {
                prefab.Spawn(spawnPosition, Mathf.RoundToInt(value));
            }
        }

        private DamageNumbersPro.DamageNumber GetPrefab(DamageType type)
        {
            return type switch
            {
                DamageType.Crit => critPrefab != null ? critPrefab : normalPrefab,
                DamageType.Heal => healPrefab != null ? healPrefab : normalPrefab,
                DamageType.Miss => missPrefab != null ? missPrefab : normalPrefab,
                _ => normalPrefab
            };
        }

        private Vector3 GetSpawnPosition(Vector3 position)
        {
            Vector3 offset = defaultOffset + new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                Random.Range(0f, randomOffset.y),
                Random.Range(-randomOffset.z, randomOffset.z)
            );

            return position + offset;
        }
    }
}