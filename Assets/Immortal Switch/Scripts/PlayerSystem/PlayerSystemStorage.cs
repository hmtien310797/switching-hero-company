using Immortal_Switch.Scripts.PlayerSystem.Interfaces;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem
{
    internal class PlayerSystemStorage : IPlayerSystemStorage
    {
        /// <summary>
        /// save key localstorage.
        /// </summary>
        private const string SAVE_KEY = nameof(PlayerSystem);

        public PlayerSystemData Data { get; private set; }

        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
            Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
        }

        public void Load()
        {
            Data = ES3.KeyExists(SAVE_KEY) ? ES3.Load<PlayerSystemData>(SAVE_KEY) : new PlayerSystemData();
            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }
    }
}