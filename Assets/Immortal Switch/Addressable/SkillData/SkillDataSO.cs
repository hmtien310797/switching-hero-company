using Scripts.Battle;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill_", menuName = "ScriptableObjects/SkillDataSo")]
public class SkillDataSO : ScriptableObject
{
    public int SkillId;
    public TierSkillGroup SkillGroup;
    public TierSkill Tier;
    public int NumSpawn = 1;
    public BaseExternalSkillController SkillPrefab;
}
