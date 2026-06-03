using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [RequireComponent(typeof(LineRenderer))]
    public class SkillRadiusDebugView : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool showRuntimeCircle = true;
        [SerializeField] private bool showGizmos = true;

        [Header("Circle")]
        [SerializeField] private float radius = 2.5f;
        [SerializeField] private int segments = 64;
        [SerializeField] private float yOffset = 0.02f;

        [Header("Line")]
        [SerializeField] private float width = 0.04f;

        private LineRenderer lineRenderer;

        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
            RefreshCircle();
        }

        public void SetVisible(bool visible)
        {
            showRuntimeCircle = visible;

            if (lineRenderer != null)
                lineRenderer.enabled = visible;
        }

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();

            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = width;

            RefreshCircle();
        }

        private void OnEnable()
        {
            RefreshCircle();
        }

        private void OnValidate()
        {
            if (segments < 8)
                segments = 8;

            if (radius < 0f)
                radius = 0f;

            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                lineRenderer.useWorldSpace = false;
                lineRenderer.loop = true;
                lineRenderer.widthMultiplier = width;
                RefreshCircle();
            }
        }

        private void RefreshCircle()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.enabled = showRuntimeCircle;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = width;

            lineRenderer.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;

                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                // Vì game bạn chạy trên XZ, vòng tròn nằm ngang trên mặt đất.
                Vector3 localPos = new Vector3(x, yOffset, z);

                lineRenderer.SetPosition(i, localPos);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos)
                return;

            Gizmos.color = Color.red;
            DrawCircleGizmo(transform.position, radius, segments);
        }

        private void DrawCircleGizmo(Vector3 center, float r, int seg)
        {
            if (r <= 0f)
                return;

            Vector3 prev = center + new Vector3(r, yOffset, 0f);

            for (int i = 1; i <= seg; i++)
            {
                float angle = (float)i / seg * Mathf.PI * 2f;

                Vector3 next = center + new Vector3(
                    Mathf.Cos(angle) * r,
                    yOffset,
                    Mathf.Sin(angle) * r
                );

                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}