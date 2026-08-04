using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using Immortal_Switch.Scripts.TransmutationSystem.Models;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    internal class TransmutationSystemStorage : ITransmutationSystemStorage
    {
        public TransmutationSystemData Data { get; private set; }

        public void Save()
        {
            //ES3.Save(SAVE_KEY, Data);
            //Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
        }

        public void Load()
        {
            Data = new TransmutationSystemData();
        }
    }
}