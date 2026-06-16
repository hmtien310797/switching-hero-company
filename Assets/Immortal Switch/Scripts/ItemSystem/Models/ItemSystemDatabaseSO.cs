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

        public async UniTask<Sprite> GetCurrencyIcon(int itemId)
        {
            var item = GetItem(itemId);

            if (item != null)
            {
                // todo: chi dung de test. su dung item.iconKey thay the.
                var iconKey = "icon_diamond";
                var path = Path.Join(prefixCurrencyIcon, iconKey).Replace('\\', '/');
                Debug.Log($"Path: {path} of {itemId}");
                return await Addressables.LoadAssetAsync<Sprite>(path).ToUniTask();
            }

            return null;
        }

        public async UniTask<Sprite> GetCurrencyIcon(string itemKey)
        {
            var item = GetItem(itemKey);

            if (item != null)
            {
                // todo: chi dung de test. su dung item.iconKey thay the.
                var iconKey = "icon_diamond";
                var path = Path.Join(prefixCurrencyIcon, iconKey).Replace('\\', '/');
                Debug.Log($"Path: {path} of {itemKey}");
                return await Addressables.LoadAssetAsync<Sprite>(path).ToUniTask();
            }

            return null;
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow GetItem(int itemId)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemId == itemId);
        }

        public DynamicHeroesGlobalSpecificationsItemConfigRow GetItem(string itemKey)
        {
            return ItemConfig.rows.FirstOrDefault(v => v.itemKey == itemKey);
        }
    }
}