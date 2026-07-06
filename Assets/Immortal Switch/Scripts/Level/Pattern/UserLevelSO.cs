using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Pattern
{
    [CreateAssetMenu(fileName = "UserLevelConfig", menuName = "ScriptableObjects/UserConfig/UserLevelConfig")]
    public class UserLevelConfigSO : ScriptableObject
    {
        public UserLevelConfigPattern[] Requires;
    }

    [System.Serializable]
    public struct UserLevelConfigPattern
    {
        public int Exp;
    }
}