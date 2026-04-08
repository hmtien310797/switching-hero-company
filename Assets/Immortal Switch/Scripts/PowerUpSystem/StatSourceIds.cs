namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public static class StatSourceIds
    {
        // Applied to StatModule
        public const string PowerUpSystem = "POWERUP:SYSTEM";

        // PowerUp source ids
        public const string GrowthSystem = "POWERUP:GROWTH";
        public const string EquipmentSystem = "POWERUP:EQUIPMENT";
        public const string PetSystem = "POWERUP:PET";
        public const string ResearchSystem = "POWERUP:RESEARCH";
        public const string RelicSystem = "POWERUP:RELIC";

        // Runtime combat source prefixes
        public const string BuffPrefix = "BUFF:";
        public const string DotPrefix = "DOT:";
        public const string AuraPrefix = "AURA:";
        public const string SkillPrefix = "SKILL:";
        public const string PassivePrefix = "PASSIVE:";

        public static string Buff(string buffId)
        {
            return $"{BuffPrefix}{buffId}";
        }

        public static string Dot(string dotId)
        {
            return $"{DotPrefix}{dotId}";
        }

        public static string Aura(string auraId)
        {
            return $"{AuraPrefix}{auraId}";
        }

        public static string Skill(string skillId)
        {
            return $"{SkillPrefix}{skillId}";
        }

        public static string Passive(string passiveId)
        {
            return $"{PassivePrefix}{passiveId}";
        }

        public static bool IsBuffSource(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId) &&
                   sourceId.StartsWith(BuffPrefix, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDotSource(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId) &&
                   sourceId.StartsWith(DotPrefix, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAuraSource(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId) &&
                   sourceId.StartsWith(AuraPrefix, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSkillSource(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId) &&
                   sourceId.StartsWith(SkillPrefix, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPassiveSource(string sourceId)
        {
            return !string.IsNullOrEmpty(sourceId) &&
                   sourceId.StartsWith(PassivePrefix, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}