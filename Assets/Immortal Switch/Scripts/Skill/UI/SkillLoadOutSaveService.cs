using System.Collections.Generic;
using System.Linq;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public static class SkillLoadoutSaveService
    {
        private const string SaveFile = "SkillLoadout.es3";
        private const string HeroSkillKeyPrefix = "hero_selected_skills_";

        private static string GetHeroKey(int heroId) => $"{HeroSkillKeyPrefix}{heroId}";

        public static List<int> GetSelectedSkillIdsByHeroId(int heroId)
        {
            if (heroId <= 0)
                return new List<int>();

            string key = GetHeroKey(heroId);
            if (!ES3.KeyExists(key, SaveFile))
                return new List<int>();

            var list = ES3.Load(key, SaveFile, new List<int>());
            return list?.Where(x => x > 0).Distinct().Take(5).ToList() ?? new List<int>();
        }

        public static void SaveSelectedSkillIdsByHeroId(int heroId, List<int> skillIds)
        {
            if (heroId <= 0)
                return;

            var normalized = (skillIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .Take(5)
                .ToList();

            ES3.Save(GetHeroKey(heroId), normalized, SaveFile);
        }

        public static void ClearSelectedSkillIdsByHeroId(int heroId)
        {
            if (heroId <= 0)
                return;

            string key = GetHeroKey(heroId);
            if (ES3.KeyExists(key, SaveFile))
                ES3.DeleteKey(key, SaveFile);
        }
    }
}