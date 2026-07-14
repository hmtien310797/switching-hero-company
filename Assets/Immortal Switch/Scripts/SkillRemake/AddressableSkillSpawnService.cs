using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public static class AddressableSkillSpawnService
    {
        public static Action<int> OnSkillDespawned;
        public static async UniTask PrewarmSkillRuntimeAssetsAsync(SkillDataSO skillData)
        {
            int level = 1;
            if (skillData.OwnerType == SkillOwnerType.ClassSkill)
            {
                UserDataCache.Instance.GetServerSkillLevel(skillData.SkillId);
            }
            
            switch (skillData.RuntimeObjectConfig.RuntimeVisualType)
            {
                case SkillRuntimeVisualType.SpawnedSkillObject:
                case SkillRuntimeVisualType.HeroSpineAndSpawnedSkillObject:
                    if (skillData.RuntimeObjectConfig.SpawnMode == SkillRuntimeSpawnMode.AddressablePool)
                    {
                        await AddressablePoolService.Instance.CreatePoolAsync(
                            skillData.RuntimeObjectConfig.RuntimeAddressableKey, 1);
                        return;
                    }
                    await AddressablePoolService.Instance.CreatePoolAsync(
                        skillData.RuntimeObjectConfig.MultiSpawnConfig.ChildRuntimeAddressableKey, skillData.RuntimeObjectConfig.MultiSpawnConfig.SpawnCount);
                    return;
            }
            
            if (skillData.OwnerType == SkillOwnerType.ClassSkill)
            {
                switch (skillData.RuntimeObjectConfig.RuntimeVisualType)
                {
                    case SkillRuntimeVisualType.SpawnHomingProjectile:
                    case  SkillRuntimeVisualType.HeroSpineObjectAndHomingProjectile:    
                        await AddressablePoolService.Instance.CreatePoolAsync(
                            skillData.BasePhases[0].Actions[0].Projectile.HomingChainBulletConfig.bulletAddressableKey, skillData.BasePhases[0].Actions[0].Projectile.HomingChainBulletConfig.bulletCount);
                        break;
                    
                    case SkillRuntimeVisualType.HeroSpineObjectAndProjectile:
                    case SkillRuntimeVisualType.SpawnProjectilePatternBehavior:
                        await AddressablePoolService.Instance.CreatePoolAsync(
                            skillData.BasePhases[0].Actions[0].Projectile.BulletPatternConfig.bulletAddressableKey, skillData.BasePhases[0].Actions[0].Projectile.BulletPatternConfig.bulletsPerWave * skillData.BasePhases[0].Actions[0].Projectile.BulletPatternConfig.totalWaves);
                        break;
                }
            }
            else
            {
                switch (skillData.RuntimeObjectConfig.RuntimeVisualType)
                {
                    case SkillRuntimeVisualType.SpawnHomingProjectile:
                    case  SkillRuntimeVisualType.HeroSpineObjectAndHomingProjectile:    
                        await AddressablePoolService.Instance.CreatePoolAsync(
                            skillData.Levels[level-1].Phases[0].Actions[0].Projectile.HomingChainBulletConfig.bulletAddressableKey, skillData.Levels[level-1].Phases[0].Actions[0].Projectile.HomingChainBulletConfig.bulletCount);
                        break;
                    
                    case SkillRuntimeVisualType.HeroSpineObjectAndProjectile:
                    case SkillRuntimeVisualType.SpawnProjectilePatternBehavior:
                        await AddressablePoolService.Instance.CreatePoolAsync(
                            skillData.Levels[level-1].Phases[0].Actions[0].Projectile.BulletPatternConfig.bulletAddressableKey, skillData.Levels[level-1].Phases[0].Actions[0].Projectile.BulletPatternConfig.bulletsPerWave * Mathf.CeilToInt(skillData.Levels[level-1].Phases[0].Actions[0].Projectile.BulletPatternConfig.bulletLifeTime) * 3);
                        break;
                }
            }
        }
        
        public static void DisposeSkillComponent(SkillDataSO skillData)
        {
            OnSkillDespawned?.Invoke(skillData.SkillId);
            
            switch (skillData.RuntimeObjectConfig.RuntimeVisualType)
            {
                case SkillRuntimeVisualType.SpawnedSkillObject:
                case SkillRuntimeVisualType.HeroSpineAndSpawnedSkillObject:
                    AddressablePoolService.Instance.DespawnAndDisposePool(
                        skillData.RuntimeObjectConfig.SpawnMode == SkillRuntimeSpawnMode.AddressablePool
                            ? skillData.RuntimeObjectConfig.RuntimeAddressableKey
                            : skillData.RuntimeObjectConfig.MultiSpawnConfig.ChildRuntimeAddressableKey);

                    return;
            }

            if (skillData.OwnerType == SkillOwnerType.ClassSkill)
            {
                switch (skillData.RuntimeObjectConfig.RuntimeVisualType)
                {
                    case SkillRuntimeVisualType.SpawnHomingProjectile:
                    case  SkillRuntimeVisualType.HeroSpineObjectAndHomingProjectile:    
                        AddressablePoolService.Instance.DespawnAndDisposePool(
                            skillData.BasePhases[0].Actions[0].Projectile.HomingChainBulletConfig.bulletAddressableKey);
                        break;
                    
                    case SkillRuntimeVisualType.HeroSpineObjectAndProjectile:
                    case SkillRuntimeVisualType.SpawnProjectilePatternBehavior:
                        AddressablePoolService.Instance.DespawnAndDisposePool(
                            skillData.BasePhases[0].Actions[0].Projectile.BulletPatternConfig.bulletAddressableKey);
                        break;
                }
            }
            else
            {
                switch (skillData.RuntimeObjectConfig.RuntimeVisualType)
                {
                    case SkillRuntimeVisualType.SpawnHomingProjectile:
                    case  SkillRuntimeVisualType.HeroSpineObjectAndHomingProjectile:    
                        AddressablePoolService.Instance.DespawnAndDisposePool(
                            skillData.Levels[0].Phases[0].Actions[0].Projectile.HomingChainBulletConfig.bulletAddressableKey);
                        break;
                    
                    case SkillRuntimeVisualType.HeroSpineObjectAndProjectile:
                    case SkillRuntimeVisualType.SpawnProjectilePatternBehavior:
                        AddressablePoolService.Instance.DespawnAndDisposePool(
                            skillData.Levels[0].Phases[0].Actions[0].Projectile.BulletPatternConfig.bulletAddressableKey);
                        break;
                    
                }
            }
        }
    }
}