using Battle;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Battle
{
    public sealed class DungeonDebugEnterController : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private BattleFlowController battleFlowController;

        [Title("Dungeon Debug Data")]
        [SerializeField, MinValue(1)]
        [Tooltip("Dungeon ID có mode KillAllEnemies.")]
        private int dungeonId = 1;

        [SerializeField, MinValue(1)]
        private int dungeonStage = 1;

        [ShowInInspector, ReadOnly]
        private bool isEntering;

        [ShowInInspector, ReadOnly]
        private string currentFlowState;

        private void Update()
        {
            RefreshDebugState();
        }

        [Button(
            "Enter Kill All Dungeon",
            ButtonSizes.Large
        )]
        [EnableIf(nameof(CanEnterDungeon))]
        private void EnterDungeon()
        {
            EnterDungeonAsync().Forget();
        }

        private async UniTaskVoid EnterDungeonAsync()
        {
            if (!CanEnterDungeon())
            {
                return;
            }

            isEntering = true;
            RefreshDebugState();

            try
            {
                bool entered =
                    await battleFlowController
                        .EnterDungeonAsync(
                            dungeonId,
                            dungeonStage
                        );

                if (!entered)
                {
                    Debug.LogWarning(
                        "[DungeonDebug] " +
                        $"Không thể vào Dungeon. " +
                        $"DungeonId={dungeonId}, " +
                        $"Stage={dungeonStage}",
                        this
                    );

                    return;
                }

                Debug.Log(
                    "[DungeonDebug] " +
                    $"Đã vào Dungeon. " +
                    $"DungeonId={dungeonId}, " +
                    $"Stage={dungeonStage}",
                    this
                );
            }
            finally
            {
                isEntering = false;
                RefreshDebugState();
            }
        }

        private bool CanEnterDungeon()
        {
            return Application.isPlaying &&
                   !isEntering &&
                   battleFlowController != null &&
                   !battleFlowController.IsDungeonLocked;
        }

        private void RefreshDebugState()
        {
            if (battleFlowController == null)
            {
                currentFlowState =
                    "BattleFlowController chưa được gán";

                return;
            }

            currentFlowState =
                battleFlowController.State.ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            dungeonId = Mathf.Max(
                1,
                dungeonId
            );

            dungeonStage = Mathf.Max(
                1,
                dungeonStage
            );

            RefreshDebugState();
        }
#endif
    }
}