using Immortal_Switch.Scripts.Hero;
using UnityEngine;

public class HeroTargetFinder : MonoBehaviour
{
    [Header("Showcase Target Finding")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int maxTargets = 64;

    private Collider[] results;

    private void Awake()
    {
        results = new Collider[maxTargets];
    }

    public HeroEnemyTarget FindNearest(Vector3 center, float range)
    {
        // Showcase version:
        // Dùng Physics.OverlapSphereNonAlloc để tránh alloc GC mỗi frame.
        //
        // Sau này nên thay bằng:
        // - EnemyRegistry quản lý toàn bộ enemy còn sống
        // - Spatial partition/grid để query nhanh hơn
        // - Không query physics liên tục mỗi hero mỗi frame

        int count = Physics.OverlapSphereNonAlloc(
            center,
            range,
            results,
            enemyLayer
        );

        HeroEnemyTarget nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = results[i];
            if (col == null)
                continue;

            if (!col.TryGetComponent(out HeroEnemyTarget target))
                continue;

            if (target.IsDead)
                continue;

            float sqrDistance = (target.transform.position - center).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = target;
            }
        }

        return nearest;
    }
}