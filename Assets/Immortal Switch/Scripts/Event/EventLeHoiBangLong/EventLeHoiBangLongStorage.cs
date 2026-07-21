using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Interfaces;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong
{
    internal class EventLeHoiBangLongStorage : IEventLeHoiBangLongStorage
    {
        private const string SAVE_KEY = nameof(EventLeHoiBangLongData);

        public EventLeHoiBangLongData Data { get; private set; }

        public void Load()
        {
            Data = ES3.KeyExists(SAVE_KEY)
                ? ES3.Load<EventLeHoiBangLongData>(SAVE_KEY)
                : new EventLeHoiBangLongData();

            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }

        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
        }
    }
}
