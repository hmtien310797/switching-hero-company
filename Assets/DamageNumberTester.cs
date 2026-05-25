using UnityEngine;

public class DamageNumberTester : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject damageNumberPrefab;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0.3f, 0f);

    [Header("Test Value")]
    [SerializeField] private int minDamage = 100;
    [SerializeField] private int maxDamage = 9999;

    [Header("Input")]
    [SerializeField] private KeyCode normalDamageKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode critDamageKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode healKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode missKey = KeyCode.Alpha4;

    private void Update()
    {
        if (Input.GetKeyDown(normalDamageKey))
        {
            SpawnDamageNumber(DamageNumberTestType.Normal);
        }

        if (Input.GetKeyDown(critDamageKey))
        {
            SpawnDamageNumber(DamageNumberTestType.Crit);
        }

        if (Input.GetKeyDown(healKey))
        {
            SpawnDamageNumber(DamageNumberTestType.Heal);
        }

        if (Input.GetKeyDown(missKey))
        {
            SpawnDamageNumber(DamageNumberTestType.Miss);
        }
    }

    private void SpawnDamageNumber(DamageNumberTestType type)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("[DamageNumberTester] Damage number prefab is missing.");
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;

        Vector3 offset = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(0f, randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z)
        );

        Vector3 spawnPosition = point.position + offset;

        GameObject obj = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);

        int value = Random.Range(minDamage, maxDamage + 1);

        ApplyText(obj, type, value);
    }

    private void ApplyText(GameObject obj, DamageNumberTestType type, int value)
    {
        string text = type switch
        {
            DamageNumberTestType.Normal => value.ToString(),
            DamageNumberTestType.Crit => $"CRIT {value}",
            DamageNumberTestType.Heal => $"+{value}",
            DamageNumberTestType.Miss => "MISS",
            _ => value.ToString()
        };

        // TextMeshPro 3D
        TMPro.TextMeshPro tmp3D = obj.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp3D != null)
        {
            tmp3D.text = text;
            return;
        }

        // TextMeshPro UI
        TMPro.TextMeshProUGUI tmpUI = obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpUI != null)
        {
            tmpUI.text = text;
            return;
        }

        // Unity Legacy Text
        UnityEngine.UI.Text legacyText = obj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
            return;
        }

        Debug.LogWarning("[DamageNumberTester] Cannot find any text component on prefab.");
    }
}

public enum DamageNumberTestType
{
    Normal,
    Crit,
    Heal,
    Miss
}