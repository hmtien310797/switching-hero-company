using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Immortal_Switch.Scripts.ItemSystem.Models
{
    [CreateAssetMenu(fileName = "ItemSystemDatabase", menuName = "ScriptableObjects/ItemSystem/Database")]
    public class ItemSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// item config
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsItemConfigDatabase ItemConfig { get; private set; }

        /// <summary>
        /// duong dan chua icon
        /// </summary>
        [SerializeField] private string prefixCurrencyIcon;

        public async UniTask<Sprite> LoadCurrencyIconByItemId(int itemId)
        {
            var item = FindItem(itemId);

            if (item != null)
            {
                // todo: chi dung de test. su dung item.iconKey thay the.
                var iconKey = "icon_diamond";
                return await LoadIcon(iconKey);
            }

            return null;
        }

        public async UniTask<Sprite> LoadCurrencyIconByKey(string itemKey)
        {
            var item = FindItem(itemKey);

            if (item != null)
            {
                // todo: chi dung de test. su dung item.iconKey thay the.
                var iconKey = "icon_diamond";
                return await LoadIcon(iconKey);
            }

            return null;
        }

        public async UniTask<Sprite> LoadIcon(string iconKey)
        {
            var path = Path.Join(prefixCurrencyIcon, iconKey).Replace('\\', '/');
            return await Addressables.LoadAssetAsync<Sprite>(path).ToUniTask();
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow FindItem(int itemId)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemId == itemId);
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow FindItem(string itemKey)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemKey == itemKey);
        }
    }
}