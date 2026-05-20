using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    public class HeroEnemyTarget : MonoBehaviour
    {
        [SerializeField] private float hp = 10f;

        public bool IsDead { get; private set; }

        public Transform CachedTransform { get; private set; }

        private void Awake()
        {
            CachedTransform = transform;
        }

        public void ReceiveDamage(float damage)
        {
            if (IsDead)
                return;

            hp -= damage;

            if (hp <= 0f)
            {
                IsDead = true;
                gameObject.SetActive(false);
            }
        }
    }
}