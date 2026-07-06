using Immortal_Switch.Scripts.Shop.Interfaces;
using Immortal_Switch.Scripts.Shop.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop
{
    internal class ShopStorage : IShopStorage
    {
        private const string SAVE_KEY = nameof(ShopData);

        public ShopData Data { get; private set; }

        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
            Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
        }

        public void Load()
        {
            Data = ES3.KeyExists(SAVE_KEY)
                ? ES3.Load<ShopData>(SAVE_KEY)
                : new ShopData();

            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }
    }
}