using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Dungeon
{
    [CreateAssetMenu(
        fileName = "DungeonDatabase",
        menuName = "Immortal Switch/Dungeon/Dungeon Database")]
    public sealed class DungeonDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DungeonDefinitionData> definitions = new();
        [SerializeField] private List<DungeonStageFormulaRow> stageFormulaRows = new();
        [SerializeField] private List<DungeonDamageThresholdRow> damageThresholdRows = new();

        public IReadOnlyList<DungeonDefinitionData> Definitions => definitions;
        public IReadOnlyList<DungeonStageFormulaRow> StageFormulaRows => stageFormulaRows;
        public IReadOnlyList<DungeonDamageThresholdRow> DamageThresholdRows => damageThresholdRows;

        public bool TryGetDefinition(int dungeonId, out DungeonDefinitionData definition)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                DungeonDefinitionData current = definitions[i];
                if (current != null && current.DungeonId == dungeonId)
                {
                    definition = current;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public DungeonStageFormulaRow FindStageFormula(string tableKey, int currentStage)
        {
            DungeonStageFormulaRow selected = null;
            int selectedStage = int.MinValue;

            for (int i = 0; i < stageFormulaRows.Count; i++)
            {
                DungeonStageFormulaRow row = stageFormulaRows[i];
                if (row == null ||
                    !string.Equals(row.TableKey, tableKey, StringComparison.OrdinalIgnoreCase) ||
                    row.Stage > currentStage ||
                    row.Stage <= selectedStage)
                {
                    continue;
                }

                selected = row;
                selectedStage = row.Stage;
            }

            return selected;
        }

        public DungeonDamageThresholdRow FindDamageThresholdFormula(
            string tableKey,
            int currentStage)
        {
            DungeonDamageThresholdRow selected = null;
            int selectedStage = int.MinValue;

            for (int i = 0; i < damageThresholdRows.Count; i++)
            {
                DungeonDamageThresholdRow row = damageThresholdRows[i];
                if (row == null ||
                    !string.Equals(row.TableKey, tableKey, StringComparison.OrdinalIgnoreCase) ||
                    row.Stage > currentStage ||
                    row.Stage <= selectedStage)
                {
                    continue;
                }

                selected = row;
                selectedStage = row.Stage;
            }

            return selected;
        }
        
        public int GetDungeonMaxStage(int dungeonId)
        {
            return TryGetDefinition(
                dungeonId,
                out DungeonDefinitionData definition)
                ? Mathf.Max(0, definition.StageCount)
                : 0;
        }
        
        public string GetDungeonDisplayName(int dungeonId)
        {
            return TryGetDefinition(
                dungeonId,
                out DungeonDefinitionData definition)
                ? definition.UiNameVi
                : string.Empty;
        }
        
        public int GetDungeonTicketRequest(int dungeonId)
        {
            return TryGetDefinition(
                dungeonId,
                out DungeonDefinitionData definition)
                ? definition.EntryCostAmount
                : 0;
        }

#if UNITY_EDITOR
        public void EditorReplaceData(
            List<DungeonDefinitionData> newDefinitions,
            List<DungeonStageFormulaRow> newStageFormulaRows,
            List<DungeonDamageThresholdRow> newDamageThresholdRows)
        {
            definitions = newDefinitions ?? new List<DungeonDefinitionData>();
            stageFormulaRows = newStageFormulaRows ?? new List<DungeonStageFormulaRow>();
            damageThresholdRows = newDamageThresholdRows ?? new List<DungeonDamageThresholdRow>();
        }
#endif
    }
}
