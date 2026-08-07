using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventBingo.UI
{
    public class UIEventBingoBoardPanel : MonoBehaviour
    {
        [Header("Board references")]
        [SerializeField]
        private RectTransform boardContainer;

        [SerializeField]
        private UIEventBingoTrackTile trackTilePrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIEventBingoTrackTile> _pool;
        private readonly Dictionary<int, UIEventBingoTrackTile> _tilesByTrackIndex = new();

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventBingoBoardRow> boardRows,
            IReadOnlyCollection<int> unlockedTrackIndices
        )
        {
            RefreshBoard(boardRows, unlockedTrackIndices);
        }

        private void RefreshBoard(
            List<DynamicHeroesGlobalSpecificationsEventBingoBoardRow> boardRows,
            IReadOnlyCollection<int> unlockedTrackIndices
        )
        {
            _pool ??= new SimpleUIPool<UIEventBingoTrackTile>(trackTilePrefab, boardContainer);
            _tilesByTrackIndex.Clear();

            for (int i = 0; i < boardRows.Count; i++)
            {
                var row = boardRows[i];
                var clone = _pool.Get(i);
                var isUnlocked = unlockedTrackIndices.Contains(row.trackIndex);

                clone.Bind(row.rewardId, row.rewardAmount, !isUnlocked);
                clone.SetClaimed(isUnlocked);

                _tilesByTrackIndex[row.trackIndex] = clone;
            }

            _pool.ReleaseFrom(boardRows.Count);
        }

        /// <summary>Chạy animation mở tile theo track index.</summary>
        public async UniTask<bool> RevealTileAsync(
            int trackIndex,
            float duration,
            CancellationToken cancellationToken
        )
        {
            if (!_tilesByTrackIndex.TryGetValue(trackIndex, out var tile))
            {
                return false;
            }

            await tile.PlayRevealAsync(duration, cancellationToken);
            return true;
        }

        /// <summary>Cập nhật trạng thái claimed của tile theo track index.</summary>
        public void SetTileClaimed(int trackIndex, bool value)
        {
            if (_tilesByTrackIndex.TryGetValue(trackIndex, out var tile))
            {
                tile.SetClaimed(value);
            }
        }
    }
}