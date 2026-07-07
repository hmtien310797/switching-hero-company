using Immortal_Switch.Scripts.Shop.Models;

namespace Immortal_Switch.Scripts.Shop.Interfaces
{
    public interface IShopStorage
    {
        /// <summary>
        /// save data
        /// </summary>
        ShopData Data { get; }

        /// <summary>
        /// save data
        /// </summary>
        void Save();

        /// <summary>
        /// load data
        /// </summary>
        void Load();
    }
}