using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using UnityEngine;

namespace Battle
{
    public sealed class PvEMapController : MonoBehaviour
    {
        private const string MapAddressPrefix =
            "Assets/Immortal Switch/Addressable/Map/Prefab";

        private PvEMapView currentMapView;
        private string currentMapKey;

        public string CurrentMapKey =>
            currentMapKey;

        public PvEMapView CurrentMapView =>
            currentMapView;

        public bool HasCurrentMap =>
            currentMapView != null;

        public Vector3 GetEndMapPosition()
        {
            return currentMapView != null
                ? currentMapView.GetEndMapPoint
                : Vector3.zero;
        }

        /// <summary>
        /// Giữ tương thích với logic Chapter hiện tại.
        /// chapterIndex là index đang được dùng trong project.
        /// </summary>
        public UniTask<PvEMapView> InitChapterMapAsync(
            int chapterIndex)
        {
            string mapKey =
                $"map{chapterIndex + 1}";

            return InitMapAsync(mapKey);
        }

        /// <summary>
        /// Spawn map Dungeon theo map_name trong Dungeon_Definition.
        /// </summary>
        public UniTask<PvEMapView> InitDungeonMapAsync(
            string mapName)
        {
            return InitMapAsync(mapName);
        }

        /// <summary>
        /// Có thể dùng chung nếu nơi gọi đã có map key hoàn chỉnh.
        /// </summary>
        public async UniTask<PvEMapView> InitMapAsync(
            string mapKey)
        {
            if (string.IsNullOrWhiteSpace(mapKey))
            {
                Debug.LogError(
                    "[PvEMapController] mapKey is empty.",
                    this
                );

                return null;
            }

            // Đang sử dụng đúng map rồi thì không spawn lại.
            if (currentMapView != null &&
                string.Equals(
                    currentMapKey,
                    mapKey,
                    StringComparison.Ordinal))
            {
                return currentMapView;
            }

            ReleaseCurrentMap();

            currentMapKey = mapKey;

            currentMapView =
                await AddressableSpawnService
                    .SpawnAsync<PvEMapView>(
                        MapAddressPrefix,
                        mapKey,
                        Vector3.zero,
                        Quaternion.identity,
                        transform
                    );

            if (currentMapView != null)
            {
                return currentMapView;
            }

            Debug.LogError(
                $"[PvEMapController] " +
                $"Cannot spawn map '{mapKey}'.",
                this
            );

            currentMapKey = null;

            return null;
        }

        public void ReleaseCurrentMap()
        {
            if (currentMapView != null)
            {
                AddressableSpawnService.ReleaseAsset(
                    currentMapView
                );

                currentMapView = null;
            }

            currentMapKey = null;
        }

        private void OnDestroy()
        {
            ReleaseCurrentMap();
        }
    }
}