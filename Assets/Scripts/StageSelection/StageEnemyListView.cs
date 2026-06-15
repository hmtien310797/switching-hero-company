using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageEnemyListView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StageEnemyItemView itemPrefab;
        
        public async UniTask Bind(int[] enemyIds, int bossId)
        {
            Clear();

            if (enemyIds != null)
            {
                for (int i = 0; i < enemyIds.Length; i++)
                {
                    int enemyId = enemyIds[i];

                    StageEnemyItemView item = Instantiate(itemPrefab, contentRoot);

                    Sprite icon = null;
                    
                    if(MasterDataCache.Instance.TryGetCreepData(enemyId, out CreepDataSo creepData))
                    {
                        icon = await AddressableSpawnService.LoadSpriteAsync(creepData.IconKey);
                    }

                    item.Bind(enemyId, icon, isBoss: false);
                }
            }

            if (bossId > 0)
            {
                StageEnemyItemView bossItem = Instantiate(itemPrefab, contentRoot);

                Sprite bossIcon = null;
                
                if(MasterDataCache.Instance.TryGetBossData(bossId, out BossDataSO bossDataSo))
                {
                    bossIcon = bossDataSo.Icon;
                }

                bossItem.Bind(bossId, bossIcon, isBoss: true);
            }
        }

        private void Clear()
        {
            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}