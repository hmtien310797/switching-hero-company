using System.Collections.Generic;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;

namespace Immortal_Switch.Scripts.Combat
{
    [RequireComponent(typeof(Collider))]
    public class AttractArea : MonoBehaviour
    {
        [Header("Attract Settings")]
        [SerializeField, Min(0f)]
        private float attractSpeed = 5f;

        [SerializeField, Min(0f)]
        private float centerStopDistance = 0.15f;

        [SerializeField]
        private bool faceMovementDirection = true;

        [SerializeField]
        private Transform attractCenter;

        [Header("Lifetime")]
        [SerializeField]
        private bool automaticallyReleaseOnDisable = true;

        private readonly HashSet<EnemyActor> affectedEnemies =
            new();

        private Vector3 CenterPosition =>
            attractCenter != null
                ? attractCenter.position
                : transform.position;

        private void Awake()
        {
            Collider triggerCollider =
                GetComponent<Collider>();

            triggerCollider.isTrigger = true;
        }

        private void FixedUpdate()
        {
            UpdateAffectedEnemies();
        }

        private void OnTriggerEnter(Collider other)
        {
            EnemyActor enemy =
                other.GetComponentInParent<EnemyActor>();

            if (!IsValidEnemy(enemy))
                return;

            if (!affectedEnemies.Add(enemy))
                return;

            enemy.BeginExternalMovement(
                this,
                CenterPosition,
                attractSpeed,
                centerStopDistance,
                faceMovementDirection
            );
        }

        private void OnTriggerExit(Collider other)
        {
            EnemyActor enemy =
                other.GetComponentInParent<EnemyActor>();

            if (enemy == null)
                return;

            ReleaseEnemy(enemy);
        }

        private void UpdateAffectedEnemies()
        {
            if (affectedEnemies.Count == 0)
                return;

            affectedEnemies.RemoveWhere(
                enemy =>
                {
                    if (!IsValidEnemy(enemy))
                        return true;

                    enemy.UpdateExternalMovement(
                        this,
                        CenterPosition,
                        attractSpeed
                    );

                    return false;
                });
        }

        private void ReleaseEnemy(EnemyActor enemy)
        {
            if (enemy == null)
                return;

            if (!affectedEnemies.Remove(enemy))
                return;

            enemy.EndExternalMovement(this);
        }

        private void ReleaseAllEnemies()
        {
            foreach (EnemyActor enemy in affectedEnemies)
            {
                if (enemy == null)
                    continue;

                enemy.EndExternalMovement(this);
            }

            affectedEnemies.Clear();
        }

        private bool IsValidEnemy(EnemyActor enemy)
        {
            return enemy != null &&
                   !enemy.IsDead &&
                   enemy.gameObject.activeInHierarchy;
        }

        private void OnDisable()
        {
            if (automaticallyReleaseOnDisable)
                ReleaseAllEnemies();
        }

        private void OnDestroy()
        {
            ReleaseAllEnemies();
        }
    }
}