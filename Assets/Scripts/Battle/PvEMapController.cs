using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class PvEMapController : MonoBehaviour
    {
        [SerializeField] List<PvEMapView> maps;
        
        private Dictionary<int, PvEMapView> mapViewDict = new Dictionary<int,PvEMapView>();
        private PvEMapView curMapView;

        public int curChapterId = -1;

        private void Awake()
        {
            var mapCount = maps.Count;
            for (int i = 0; i < mapCount; i++)
            {
                mapViewDict[i] = maps[i];
            }
        }

        public Vector3 GetEndMapPosition()
        {
            return curMapView.GetEndMapPoint;
        }

        public void InitMapByChapter(int chapter)
        {
            if (chapter == curChapterId || !mapViewDict.ContainsKey(chapter)) return;

            curChapterId = chapter;
            if(curMapView)
            {
                Destroy(curMapView);
                Destroy(curMapView.gameObject);
            }

            curMapView = Instantiate(mapViewDict[curChapterId], transform);
            curMapView.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        }
        
    }
}
