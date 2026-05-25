
using UnityEngine;

namespace UI
{
    public class HealthBarController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        public void SetHealth(float health)
        {
            var size = spriteRenderer.size;
            size.x = health;
            spriteRenderer.size = size;
        }

        public void PreSetHealth()
        {
            var size = spriteRenderer.size;
            size.x = 1;
            spriteRenderer.size = size;
        }
    }
}
