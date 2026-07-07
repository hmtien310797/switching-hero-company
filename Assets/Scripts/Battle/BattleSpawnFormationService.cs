using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    /// <summary>
    /// Chỉ chịu trách nhiệm tạo danh sách vị trí đội hình từ các spawn anchor.
    /// Không spawn enemy và không giữ state riêng của Chapter/Dungeon.
    /// </summary>
    public sealed class BattleSpawnFormationService : MonoBehaviour
    {
        [Header("Group Count")]
        [SerializeField, Min(1)] private int minSpawnGroupsPerBatch = 3;
        [SerializeField, Min(1)] private int maxSpawnGroupsPerBatch = 6;

        [Header("Formation")]
        [SerializeField, Min(1)] private int columnsPerGroup = 3;
        [SerializeField, Min(0f)] private float spacingX = 1.6f;
        [SerializeField, Min(0f)] private float spacingZ = 1.25f;
        [SerializeField, Min(0f)] private float jitter = 0.25f;

        private readonly List<Transform> anchorCandidates = new();
        private readonly List<Transform> selectedAnchors = new();
        private readonly List<int> groupCounts = new();

        public bool GeneratePositions(
            int amount,
            IReadOnlyList<Transform> spawnAnchors,
            List<Vector3> result)
        {
            if (result == null)
            {
                Debug.LogError("[SpawnFormation] Result list is null.", this);
                return false;
            }

            result.Clear();

            if (amount <= 0)
                return true;

            CollectValidAnchors(spawnAnchors);
            if (anchorCandidates.Count == 0)
            {
                Debug.LogError("[SpawnFormation] No valid spawn anchor.", this);
                return false;
            }

            int maxPossibleGroups = Mathf.Min(anchorCandidates.Count, amount);
            int minGroups = Mathf.Clamp(minSpawnGroupsPerBatch, 1, maxPossibleGroups);
            int maxGroups = Mathf.Clamp(maxSpawnGroupsPerBatch, minGroups, maxPossibleGroups);
            int groupCount = Random.Range(minGroups, maxGroups + 1);

            SelectRandomAnchors(groupCount);
            AllocateEvenCounts(amount, selectedAnchors.Count);

            for (int groupIndex = 0; groupIndex < selectedAnchors.Count; groupIndex++)
            {
                AddGroupPositions(
                    result,
                    selectedAnchors[groupIndex].position,
                    groupCounts[groupIndex]
                );
            }

            Shuffle(result);
            return result.Count == amount;
        }

        private void CollectValidAnchors(IReadOnlyList<Transform> spawnAnchors)
        {
            anchorCandidates.Clear();
            selectedAnchors.Clear();
            groupCounts.Clear();

            if (spawnAnchors == null)
                return;

            for (int i = 0; i < spawnAnchors.Count; i++)
            {
                Transform anchor = spawnAnchors[i];
                if (anchor != null)
                    anchorCandidates.Add(anchor);
            }
        }

        private void SelectRandomAnchors(int count)
        {
            count = Mathf.Min(count, anchorCandidates.Count);

            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(0, anchorCandidates.Count);
                selectedAnchors.Add(anchorCandidates[randomIndex]);
                anchorCandidates.RemoveAt(randomIndex);
            }
        }

        private void AllocateEvenCounts(int totalAmount, int groupCount)
        {
            groupCounts.Clear();

            int baseCount = totalAmount / groupCount;
            int remainder = totalAmount % groupCount;

            for (int i = 0; i < groupCount; i++)
                groupCounts.Add(baseCount + (i < remainder ? 1 : 0));

            Shuffle(groupCounts);
        }

        private void AddGroupPositions(
            List<Vector3> result,
            Vector3 anchorPosition,
            int amount)
        {
            int columns = Mathf.Max(1, columnsPerGroup);
            int rows = Mathf.CeilToInt(amount / (float)columns);
            float totalWidth = (columns - 1) * spacingX;
            float totalDepth = (rows - 1) * spacingZ;

            for (int i = 0; i < amount; i++)
            {
                int row = i / columns;
                int column = i % columns;

                float x = column * spacingX - totalWidth * 0.5f;
                float z = row * spacingZ - totalDepth * 0.5f;

                if ((row & 1) == 1)
                    x += spacingX * 0.5f;

                Vector3 randomOffset = new(
                    Random.Range(-jitter, jitter),
                    0f,
                    Random.Range(-jitter, jitter)
                );

                result.Add(anchorPosition + new Vector3(x, 0f, z) + randomOffset);
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minSpawnGroupsPerBatch = Mathf.Max(1, minSpawnGroupsPerBatch);
            maxSpawnGroupsPerBatch = Mathf.Max(minSpawnGroupsPerBatch, maxSpawnGroupsPerBatch);
            columnsPerGroup = Mathf.Max(1, columnsPerGroup);
            spacingX = Mathf.Max(0f, spacingX);
            spacingZ = Mathf.Max(0f, spacingZ);
            jitter = Mathf.Max(0f, jitter);
        }
#endif
    }
}
