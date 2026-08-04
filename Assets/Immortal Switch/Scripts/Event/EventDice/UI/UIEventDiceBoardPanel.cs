using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventDice.UI
{
    public class UIEventDiceBoardPanel : MonoBehaviour
    {
        private const int MINIMUM_GRID_DIMENSION = 2;
        private const int RECTANGLE_EDGE_PAIR_COUNT = 2;
        private const int RECTANGLE_CORNER_COUNT = 4;
        private const int BEZIER_SEGMENTS_PER_STEP = 6;

        [SerializeField]
        private RectTransform flag;

        [SerializeField]
        private RectTransform boardContainer;

        [SerializeField]
        private UIEventDiceTrackTile trackTilePrefab;

        [Header("Board config references")]
        [SerializeField]
        private RectOffset padding = new();

        [SerializeField]
        [Tooltip("Khoảng cách cố định giữa hai tile. Đặt 0 để các RectTransform liền nhau.")]
        private Vector2 spacing;

        [Header("Flag animation references")]
        [SerializeField]
        [Min(0f)]
        private float flagStepDuration = 0.25f;

        [SerializeField]
        [Min(0f)]
        private float flagArcHeight = 50f;

        [SerializeField]
        private Ease flagMoveEase = Ease.OutQuad;

        [SerializeField]
        [Min(MINIMUM_GRID_DIMENSION)]
        private int column = 6;

        [SerializeField]
        [Min(MINIMUM_GRID_DIMENSION)]
        private int row = 6;

        // --- Private Fields ---
        private SimpleUIPool<UIEventDiceTrackTile> _pool;
        private readonly List<RectTransform> _tileRects = new();
        private CancellationTokenSource _flagMoveCancellation;
        private Tweener _flagMoveTween;

        private int _currentIndex;

        public void Bind(List<DynamicHeroesGlobalSpecificationsEventDiceBoardRow> boardRows, int currentIndex)
        {
            _currentIndex = currentIndex;

            CancelFlagMovement();
            RefreshBoard(boardRows);

            if (_tileRects.Count > 0)
            {
                SetFlagIndex(_currentIndex);
            }
        }

        private void RefreshBoard(List<DynamicHeroesGlobalSpecificationsEventDiceBoardRow> boardRows)
        {
            _pool ??= new SimpleUIPool<UIEventDiceTrackTile>(trackTilePrefab, boardContainer);
            _tileRects.Clear();

            var columns = Mathf.Max(MINIMUM_GRID_DIMENSION, column);
            var rows = Mathf.Max(MINIMUM_GRID_DIMENSION, row);
            var perimeterCapacity = (columns + rows) * RECTANGLE_EDGE_PAIR_COUNT - RECTANGLE_CORNER_COUNT;

            var orderedRows = boardRows
                .OrderBy(boardRow => boardRow.trackIndex)
                .Take(perimeterCapacity)
                .ToList();

            if (boardRows.Count > perimeterCapacity)
            {
                Debug.LogError(
                    $"[UIEventDiceBoardPanel] Grid {columns}x{rows} chỉ chứa được " +
                    $"{perimeterCapacity} ô, nhưng config có {boardRows.Count} ô."
                );
            }

            for (var index = 0; index < orderedRows.Count; index++)
            {
                var boardRow = orderedRows[index];
                var tile = _pool.Get(index);
                var tileRect = (RectTransform)tile.transform;
                var gridPosition = GetClockwiseGridPosition(index, columns, rows);

                SetGridPosition(tileRect, gridPosition);
                tile.Bind(boardRow.rewardId, boardRow.rewardAmount, false);
                _tileRects.Add(tileRect);
            }

            _pool.ReleaseFrom(orderedRows.Count);
            flag.SetAsLastSibling();
        }

        /// <summary>
        /// Đặt flag ngay lập tức tại một index trên board.
        /// </summary>
        public void SetFlagIndex(int index)
        {
            if (_tileRects.Count == 0)
            {
                return;
            }

            _currentIndex = WrapIndex(index, _tileRects.Count);
            flag.position = _tileRects[_currentIndex].position;
        }

        /// <summary>
        /// Di chuyển flag thêm count ô theo chiều kim đồng hồ, lần lượt qua từng tile.
        /// </summary>
        public async UniTask MoveFlagAsync(int count)
        {
            if (count <= 0 ||
                _tileRects.Count == 0)
            {
                return;
            }

            CancelFlagMovement();

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );

            _flagMoveCancellation = cancellation;

            try
            {
                var path = BuildFlagPath(count, out var targetIndex);
                var duration = flagStepDuration * count;

                _flagMoveTween = flag
                    .DOPath(path.ToArray(), duration, PathType.Linear, PathMode.Ignore)
                    .SetEase(flagMoveEase)
                    .SetLink(gameObject);

                await _flagMoveTween.ToUniTask(
                    TweenCancelBehaviour.Kill,
                    cancellation.Token
                );

                _currentIndex = targetIndex;
                flag.position = _tileRects[_currentIndex].position;
            }
            catch (OperationCanceledException)
            {
                // Một lượt di chuyển mới hoặc vòng đời GameObject đã hủy animation hiện tại.
            }
            finally
            {
                if (ReferenceEquals(_flagMoveCancellation, cancellation))
                {
                    _flagMoveCancellation = null;
                    _flagMoveTween = null;
                }

                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Tính trước toàn bộ path, mỗi bước được chia thành các điểm trên cung Bezier bậc hai.
        /// </summary>
        private List<Vector3> BuildFlagPath(int count, out int targetIndex)
        {
            var path = new List<Vector3>(count * BEZIER_SEGMENTS_PER_STEP);
            var start = boardContainer.InverseTransformPoint(flag.position);
            var stepIndex = _currentIndex;

            for (var step = 0; step < count; step++)
            {
                stepIndex = WrapIndex(stepIndex + 1, _tileRects.Count);

                var end = boardContainer.InverseTransformPoint(_tileRects[stepIndex].position);
                var direction = end - start;
                var outward = new Vector3(-direction.y, direction.x, 0f).normalized;
                var control = (start + end) * 0.5f + outward * flagArcHeight;

                for (var segment = 1; segment <= BEZIER_SEGMENTS_PER_STEP; segment++)
                {
                    var progress = segment / (float)BEZIER_SEGMENTS_PER_STEP;
                    var localPosition = EvaluateQuadraticBezier(start, control, end, progress);
                    path.Add(boardContainer.TransformPoint(localPosition));
                }

                start = end;
            }

            targetIndex = stepIndex;
            return path;
        }

        /// <summary>
        /// Tính một điểm trên đường cong Bezier bậc hai.
        /// </summary>
        private static Vector3 EvaluateQuadraticBezier(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float progress
        )
        {
            var remaining = 1f - progress;
            return remaining * remaining * start + 2f * remaining * progress * control + progress * progress * end;
        }

        private static int WrapIndex(int index, int count)
        {
            return (index % count + count) % count;
        }

        private void OnDestroy()
        {
            CancelFlagMovement();
        }

        private void CancelFlagMovement()
        {
            _flagMoveCancellation?.Cancel();
            _flagMoveTween?.Kill();
            _flagMoveTween = null;
        }

        /// <summary>
        /// Đặt tile trực tiếp từ góc trên trái theo kích thước tile và spacing cố định.
        /// </summary>
        private void SetGridPosition(
            RectTransform tileRect,
            Vector2Int gridPosition
        )
        {
            var halfSize = tileRect.rect.size * 0.5f;

            tileRect.anchorMin = new Vector2(0f, 1f);
            tileRect.anchorMax = new Vector2(0f, 1f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);

            tileRect.anchoredPosition = new Vector2(
                padding.left + halfSize.x + gridPosition.x * (tileRect.rect.width + spacing.x),
                -(padding.top + halfSize.y + gridPosition.y * (tileRect.rect.height + spacing.y))
            );
        }

        /// <summary>
        /// Trả về tọa độ ô theo chiều kim đồng hồ, bắt đầu từ góc trên bên trái.
        /// </summary>
        private static Vector2Int GetClockwiseGridPosition(int index, int columns, int rows)
        {
            var lastColumn = columns - 1;
            var lastRow = rows - 1;

            if (index < columns)
            {
                return new Vector2Int(index, 0);
            }

            index -= columns;

            if (index < rows - 1)
            {
                return new Vector2Int(lastColumn, index + 1);
            }

            index -= rows - 1;

            if (index < columns - 1)
            {
                return new Vector2Int(lastColumn - 1 - index, lastRow);
            }

            index -= columns - 1;
            return new Vector2Int(0, lastRow - 1 - index);
        }
    }
}