using Scripts.Battle;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill_", menuName = "ScriptableObjects/SkillDataSo")]
public class SkillDataSO : ScriptableObject
{
    public int SkillId;
    public float CooldownTime = 10f;
    public TierSkillGroup SkillGroup;
    public TierSkill Tier;
    public int NumSpawn = 1;
    public BaseExternalSkillController SkillPrefab;
    public Sprite skillIcon;
    public float NomalDame;
    public float FinalDame;
}
