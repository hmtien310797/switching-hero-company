using System.Collections.Generic;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    /// <summary>
    /// Thuật toán chọn ngẫu nhiên một mục tiêu trong nhóm enemy xa nhất.
    /// Service không sở hữu và không chỉnh sửa danh sách actor của battle controller.
    /// </summary>
    public sealed class BattleEnemySelectionService
    {
        private readonly List<ICombatUnit> farthestCandidates = new(8);
        private readonly List<float> farthestDistances = new(8);

        public ICombatUnit GetRandomFromFarthestEnemies(
            Vector3 originPosition,
            IReadOnlyList<EnemyActor> activeEnemies,
            BossActor activeBoss,
            IReadOnlyList<ICombatUnit> excludedTargets,
            int topCount = 5,
            bool preferBoss = true)
        {
            if (topCount <= 0)
                topCount = 1;

            if (preferBoss && IsValidBoss(activeBoss, excludedTargets))
                return activeBoss;

            farthestCandidates.Clear();
            farthestDistances.Clear();

            Vector3 flattenedOrigin = originPosition;
            flattenedOrigin.y = 0f;

            if (activeEnemies != null)
            {
                for (int i = activeEnemies.Count - 1; i >= 0; i--)
                {
                    EnemyActor enemy = activeEnemies[i];

                    if (!IsValidEnemy(enemy) || IsExcluded(enemy, excludedTargets))
                        continue;

                    Vector3 enemyPosition = enemy.Position;
                    enemyPosition.y = 0f;

                    float squaredDistance =
                        (enemyPosition - flattenedOrigin).sqrMagnitude;

                    InsertCandidateByDistanceDesc(
                        enemy,
                        squaredDistance,
                        topCount
                    );
                }
            }

            if (!preferBoss && IsValidBoss(activeBoss, excludedTargets))
            {
                Vector3 bossPosition = activeBoss.Position;
                bossPosition.y = 0f;

                float bossSquaredDistance =
                    (bossPosition - flattenedOrigin).sqrMagnitude;

                InsertCandidateByDistanceDesc(
                    activeBoss,
                    bossSquaredDistance,
                    topCount
                );
            }

            if (farthestCandidates.Count == 0)
                return null;

            int randomIndex = Random.Range(0, farthestCandidates.Count);
            return farthestCandidates[randomIndex];
        }

        private void InsertCandidateByDistanceDesc(
            ICombatUnit unit,
            float squaredDistance,
            int maxCount)
        {
            int insertIndex = farthestCandidates.Count;

            for (int i = 0; i < farthestDistances.Count; i++)
            {
                if (squaredDistance > farthestDistances[i])
                {
                    insertIndex = i;
                    break;
                }
            }

            if (insertIndex >= maxCount)
                return;

            farthestCandidates.Insert(insertIndex, unit);
            farthestDistances.Insert(insertIndex, squaredDistance);

            if (farthestCandidates.Count <= maxCount)
                return;

            int lastIndex = farthestCandidates.Count - 1;
            farthestCandidates.RemoveAt(lastIndex);
            farthestDistances.RemoveAt(lastIndex);
        }

        private static bool IsValidEnemy(EnemyActor enemy)
        {
            return enemy != null &&
                   !enemy.IsDead &&
                   enemy.gameObject.activeInHierarchy;
        }

        private static bool IsValidBoss(
            BossActor boss,
            IReadOnlyList<ICombatUnit> excludedTargets)
        {
            return boss != null &&
                   !boss.IsDead &&
                   boss.gameObject.activeInHierarchy &&
                   !IsExcluded(boss, excludedTargets);
        }

        private static bool IsExcluded(
            ICombatUnit unit,
            IReadOnlyList<ICombatUnit> excludedTargets)
        {
            if (unit == null || excludedTargets == null)
                return false;

            for (int i = 0; i < excludedTargets.Count; i++)
            {
                if (excludedTargets[i] == unit)
                    return true;
            }

            return false;
        }
    }
}
