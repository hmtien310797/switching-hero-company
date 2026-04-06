using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonProbabilityPopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Content")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private SkillSummonProbabilityItemUI itemPrefab;

        private readonly List<SkillSummonProbabilityItemUI> _items = new();

        private bool _initialized;

        private void Awake()
        {
            if (root != null)
                root.SetActive(false);

            _initialized = true;
        }

        public void Show(List<SkillSummonProbabilityData> dataList)
        {
            if (!_initialized)
                Initialize();

            if (root != null)
                root.SetActive(true);

            Bind(dataList);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void Initialize()
        {
            _initialized = true;
        }

        private void Bind(List<SkillSummonProbabilityData> dataList)
        {
            Clear();

            if (dataList == null)
                return;

            for (int i = 0; i < dataList.Count; i++)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(dataList[i]);
                _items.Add(item);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    Destroy(_items[i].gameObject);
            }

            _items.Clear();
        }
    }
}