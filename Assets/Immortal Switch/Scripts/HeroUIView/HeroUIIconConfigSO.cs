using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    [CreateAssetMenu(fileName = "HeroUIIconConfig", menuName = "ScriptableObjects/UI/HeroUIIconConfig")]
    public class HeroUIIconConfigSO : ScriptableObject
    {
        public List<ElementIconEntry> ElementIcons = new();
        public List<HeroClassIconEntry> HeroClassIcons = new();

        public Sprite GetElementIcon(Element element)
        {
            var entry = ElementIcons.Find(x => x.Element == element);
            return entry != null ? entry.Icon : null;
        }

        public Sprite GetHeroClassIcon(HeroClass heroClass)
        {
            var entry = HeroClassIcons.Find(x => x.HeroClass == heroClass);
            return entry != null ? entry.Icon : null;
        }
    }

    [Serializable]
    public class ElementIconEntry
    {
        public Element Element;
        public Sprite Icon;
    }

    [Serializable]
    public class HeroClassIconEntry
    {
        public HeroClass HeroClass;
        public Sprite Icon;
    }
}