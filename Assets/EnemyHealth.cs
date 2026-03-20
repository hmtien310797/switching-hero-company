using DamageNumbersPro;
using NaughtyAttributes;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField]
    private DamageNumber damageNumber;
    
    [Button]
    public void OnDamage()
    {
        damageNumber.Spawn(transform.position);
    }
}
