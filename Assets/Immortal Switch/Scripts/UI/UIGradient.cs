using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    [RequireComponent(typeof(Image))]
    public class UIGradient : BaseMeshEffect
    {
        public Color TopColor = Color.white;
        public Color BottomColor = Color.black;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var rect = graphic.rectTransform.rect;

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                UIVertex vertex = new UIVertex();
                vh.PopulateUIVertex(ref vertex, i);

                float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                vertex.color = Color.Lerp(BottomColor, TopColor, normalizedY);

                vh.SetUIVertex(vertex, i);
            }
        }

        public void Refresh()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
    }
}