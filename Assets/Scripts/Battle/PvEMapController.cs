using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using UnityEngine;

namespace Battle
{
    public class PvEMapController : MonoBehaviour
    {
        private PvEMapView curMapView;
        private int curChapterId = -1;
        
        private const string prefix_map_address_key = "Assets/Immortal Switch/Addressable/Map/Prefab";
        
        public Vector3 GetEndMapPosition()
        {
            return curMapView.GetEndMapPoint;
        }

        public async UniTask InitMapByChapterAsync(int chapter)
        {
            if (chapter == curChapterId) return;

            curChapterId = chapter;
            if(curMapView)
            {
                AddressableSpawnService.ReleaseAsset(curMapView);
            }

            string mapName = $"map{chapter + 1}";
            curMapView = await AddressableSpawnService.SpawnAsync<PvEMapView>(prefix_map_address_key, mapName,
                new Vector3(0, 0, 0), Quaternion.identity, transform);
        }
    }
}
