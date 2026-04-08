using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Pattern
{
    [CreateAssetMenu(fileName = "ChapterStage", menuName = "ScriptableObjects/ChapterStage")]
    public class ChapterStageSO : ScriptableObject
    {
        //total stage in chaper, each chapter can have different total stage 
        public int TotalStage;
        public int BossId;
    }
}