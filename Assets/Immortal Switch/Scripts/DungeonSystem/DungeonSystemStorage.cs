using Immortal_Switch.Scripts.DungeonSystem.Interfaces;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.DungeonSystem
{
    public class DungeonSystemStorage : IDungeonSystemStorage
    {
        /// <summary>
        /// save key localstorage.
        /// </summary>
        private const string SAVE_KEY = nameof(DungeonSystem);

        public DungeonSystemData Data { get; private set; }

        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
            Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
        }

        public void Load()
        {
            Data = ES3.KeyExists(SAVE_KEY) ? ES3.Load<DungeonSystemData>(SAVE_KEY) : new DungeonSystemData();
            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }
    }
}