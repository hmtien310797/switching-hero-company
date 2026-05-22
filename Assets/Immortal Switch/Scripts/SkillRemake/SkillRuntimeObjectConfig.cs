using System;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake
{
    [Serializable]
    public class SkillRuntimeObjectConfig
    {
        public SkillRuntimeVisualType RuntimeVisualType;

        public GameObject RuntimePrefab;

        public SkillSpawnPositionType SpawnPositionType;
        public SkillFollowType FollowType;

        public Vector3 SpawnOffset;

        public float LifeTime;
        public bool DespawnOnAnimationComplete;

        public string SpineAnimationName;
        public bool LoopAnimation;
    }
}