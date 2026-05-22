
using UnityEngine;

namespace UI
{
    public class HealthBarController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] HealthTxtController healthTxtPrefab;

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

        public void ShowHealthTxt(float dame, Vector3 pos)
        {
            //var (ht,_) = PoolController.Instance.Get(healthTxtPrefab, pos);
            //ht.DoShowHealthTxt(dame, pos);
        }
    }
}
