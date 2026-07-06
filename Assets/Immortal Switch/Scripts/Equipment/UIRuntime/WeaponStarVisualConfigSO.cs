using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    [CreateAssetMenu(fileName = "WeaponStarVisualConfig", menuName = "ScriptableObjects/Equipment/WeaponStarVisualConfig")]
    public class WeaponStarVisualConfigSO : ScriptableObject
    {
        [PreviewField(50, ObjectFieldAlignment.Right)]
        public Sprite EmptyStar;
        [PreviewField(50, ObjectFieldAlignment.Right)]
        public Sprite YellowStar;
        [PreviewField(50, ObjectFieldAlignment.Right)]
        public Sprite RedStar;
        [PreviewField(50, ObjectFieldAlignment.Right)]
        public Sprite PurpleStar;
    }
}