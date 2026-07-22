using Immortal_Switch.Scripts.Tutorial.Interfaces;
using Immortal_Switch.Scripts.Tutorial.Models;

namespace Immortal_Switch.Scripts.Tutorial
{
    public class TutorialStorage : ITutorialStorage
    {
        public TutorialSaveData Data { get; private set; }

        public void Save()
        {
        }

        public void Load()
        {
            Data = new TutorialSaveData();
        }
    }
}