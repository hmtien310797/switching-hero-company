using UnityEngine;

namespace Battle
{
    public class PvEMapView : MonoBehaviour
    {
        [SerializeField] Transform endMapTrans;

        public Vector3 GetEndMapPoint => endMapTrans.position;
    }
}
