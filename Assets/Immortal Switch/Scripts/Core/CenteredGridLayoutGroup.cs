using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Core
{
    /// <summary>
    /// Grid layout tự căn giữa từng hàng.
    /// Phù hợp cho UI dạng stars, rewards, icons, badges...
    /// </summary>
    [AddComponentMenu("Layout/Centered Grid Layout Group")]
    public class CenteredGridLayoutGroup : LayoutGroup
    {
        [SerializeField] private Vector2 cellSize = new(64f, 64f);
        [SerializeField] private Vector2 spacing = new(10f, 10f);
        [SerializeField] private int columnCount = 3;

        public Vector2 CellSize
        {
            get => cellSize;
            set
            {
                cellSize = value;
                SetDirty();
            }
        }

        public Vector2 Spacing
        {
            get => spacing;
            set
            {
                spacing = value;
                SetDirty();
            }
        }

        public int ColumnCount
        {
            get => columnCount;
            set
            {
                columnCount = Mathf.Max(1, value);
                SetDirty();
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            int activeCount = GetActiveChildCount();
            int columns = Mathf.Min(columnCount, Mathf.Max(1, activeCount));

            float width =
                padding.horizontal +
                columns * cellSize.x +
                Mathf.Max(0, columns - 1) * spacing.x;

            SetLayoutInputForAxis(width, width, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int activeCount = GetActiveChildCount();
            int rows = Mathf.CeilToInt(activeCount / (float)Mathf.Max(1, columnCount));

            float height =
                padding.vertical +
                rows * cellSize.y +
                Mathf.Max(0, rows - 1) * spacing.y;

            SetLayoutInputForAxis(height, height, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            ArrangeChildren();
        }

        public override void SetLayoutVertical()
        {
            ArrangeChildren();
        }

        private void ArrangeChildren()
        {
            int activeCount = GetActiveChildCount();

            if (activeCount <= 0)
            {
                return;
            }

            int columns = Mathf.Max(1, columnCount);
            int rowCount = Mathf.CeilToInt(activeCount / (float)columns);

            float totalHeight =
                rowCount * cellSize.y +
                Mathf.Max(0, rowCount - 1) * spacing.y;

            float startY = GetStartOffset(1, totalHeight);

            int activeIndex = 0;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];

                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                int row = activeIndex / columns;
                int col = activeIndex % columns;

                int itemsInRow = GetItemsInRow(row, rowCount, activeCount, columns);

                float rowWidth =
                    itemsInRow * cellSize.x +
                    Mathf.Max(0, itemsInRow - 1) * spacing.x;

                float startX = GetStartOffset(0, rowWidth);

                float x = startX + col * (cellSize.x + spacing.x);
                float y = startY + row * (cellSize.y + spacing.y);

                SetChildAlongAxis(child, 0, x, cellSize.x);
                SetChildAlongAxis(child, 1, y, cellSize.y);

                activeIndex++;
            }
        }

        private int GetItemsInRow(
            int row,
            int rowCount,
            int activeCount,
            int columns)
        {
            bool isLastRow = row == rowCount - 1;

            if (!isLastRow)
            {
                return columns;
            }

            int remaining = activeCount - row * columns;

            return remaining <= 0 ? columns : remaining;
        }

        private int GetActiveChildCount()
        {
            int count = 0;

            foreach (RectTransform child in rectChildren)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            columnCount = Mathf.Max(1, columnCount);
            cellSize.x = Mathf.Max(0f, cellSize.x);
            cellSize.y = Mathf.Max(0f, cellSize.y);

            SetDirty();
        }
#endif
    }
}