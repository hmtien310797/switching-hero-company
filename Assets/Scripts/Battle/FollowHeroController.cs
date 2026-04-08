using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class FollowHeroController : MonoBehaviour
    {
        [SerializeField] List<Transform> points;

        private const float radiusX = 1.75f;
        private const float radiusZ = .9f;
        private const float angleDefault = 24;

        private Dictionary<int, Transform> pointDicts = new Dictionary<int, Transform>();
        private Transform followTarget = null;

        private void Awake()
        {
            InitPointDict();
        }

        private void Update()
        {
            if (followTarget == null) return;
            
            transform.position = followTarget.position;
        }

        public void SetFollowTarget(Transform trans)
        {
            followTarget = trans;
        }

        private void InitPointDict()
        {
            pointDicts.Clear();
            var idx = 0;
            foreach (Transform t in points)
            {
                var xz = new Vector3(radiusX*Mathf.Cos(angleDefault*idx), 0, radiusX /2* Mathf.Sin(angleDefault*idx));
                t.position = xz;
                pointDicts[idx] = t;
                idx++;
            }
        }

        public Transform GetNextPoint()
        {
            var rand = Random.Range(0, pointDicts.Count);
            return pointDicts[rand];
        }
    }
}
