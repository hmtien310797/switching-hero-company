using UnityEngine;

public class HeroUIView : MonoBehaviour
{
    [SerializeField] Sprite headIcon;
    [SerializeField] Sprite swithSkillIcon;

    public Sprite GetHeadIcon => headIcon;

    public Sprite SwithSkillIcon => swithSkillIcon;
}
