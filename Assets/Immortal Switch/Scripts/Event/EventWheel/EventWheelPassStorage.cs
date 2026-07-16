using System.Collections.Generic;
using Immortal_Switch.Scripts.Event.EventWheel.Interfaces;
using Immortal_Switch.Scripts.Event.EventWheel.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel
{
    /// <summary>
    /// Lưu và tải dữ liệu Event Wheel Pass bằng ES3.
    /// </summary>
    internal class EventWheelPassStorage : IEventWheelPassStorage
    {
        private const string SAVE_KEY = nameof(EventWheelPassData);

        /// <summary>
        /// Dữ liệu Event Wheel Pass hiện đang được lưu trong bộ nhớ.
        /// </summary>
        public EventWheelPassData Data { get; private set; }

        /// <summary>
        /// Lưu dữ liệu Event Wheel Pass hiện tại bằng ES3.
        /// </summary>
        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
            Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
        }

        /// <summary>
        /// Tải và chuẩn hóa dữ liệu Event Wheel Pass đã lưu bằng ES3.
        /// </summary>
        public void Load()
        {
            Data = ES3.KeyExists(SAVE_KEY)
                ? ES3.Load<EventWheelPassData>(SAVE_KEY)
                : new EventWheelPassData();

            Data ??= new EventWheelPassData();
            Data.Events ??= new Dictionary<int, EventWheelPassProgressData>();

            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }
    }
}
