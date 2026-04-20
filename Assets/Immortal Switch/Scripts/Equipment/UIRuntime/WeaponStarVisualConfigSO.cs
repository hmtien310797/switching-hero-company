using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    [CreateAssetMenu(fileName = "WeaponStarVisualConfig", menuName = "ScriptableObjects/Equipment/WeaponStarVisualConfig")]
    public class WeaponStarVisualConfigSO : ScriptableObject
    {
        public Sprite EmptyStar;
        public Sprite YellowStar;
        public Sprite RedStar;
        public Sprite PurpleStar;
    }
}