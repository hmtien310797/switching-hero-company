using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationSystemLevelInfoPanel : AnimatedUIView
    {
        [Header("References")] [SerializeField]
        private TextMeshProUGUI txtLevel;

        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnPrev;

        [SerializeField] private Transform levelInfoContainer;
        [SerializeField] private UITransmutationLevelRankInfo levelInfoPrefab;
        [SerializeField] private UITransmutationTotalStatLine statLineAvgArtifactLevel;
        [SerializeField] private UITransmutationTotalStatLine statLineCost;

        // --- Private Field ---
        private List<UITransmutationLevelRankInfo> _levelInfos = new();
        private List<DynamicHeroesGlobalSpecificationsTransmutationRateConfigRow> _rows;
        private int _currentIdx;

        private void Awake()
        {
            btnNext.onClick.AddListener(OnClickNext);
            btnPrev.onClick.AddListener(OnClickPrev);
        }

        private void OnClickNext()
        {
            OnChangeData(1);
        }

        private void OnClickPrev()
        {
            OnChangeData(-1);
        }

        private void OnChangeData(int direction)
        {
            _currentIdx = (Mathf.Max(0, _currentIdx + direction)) % _rows.Count;

            RefreshBtnDirection();
            RefreshLevel();
            RefreshVisual();
        }

        private void RefreshBtnDirection()
        {
            if (_currentIdx >= _rows.Count - 1)
            {
                btnNext.gameObject.SetActive(false);
                btnPrev.gameObject.SetActive(true);
            }
            else if (_currentIdx <= 0)
            {
                btnNext.gameObject.SetActive(true);
                btnPrev.gameObject.SetActive(false);
            }
            else
            {
                btnNext.gameObject.SetActive(true);
                btnPrev.gameObject.SetActive(true);
            }
        }

        public void Bind(List<DynamicHeroesGlobalSpecificationsTransmutationRateConfigRow> rows, int currentLevel)
        {
            _rows = rows;
            _currentIdx = rows.FindIndex(v => v.level == currentLevel);

            RefreshBtnDirection();
            RefreshLevel();
            RefreshVisual();
        }

        private void RefreshLevel()
        {
            var cfg = _rows[_currentIdx];
            txtLevel.text = cfg.level.ToString();

            statLineAvgArtifactLevel.Bind(new TransmutationSystemTotalStatEntry
            {
                IsUnique = false,
                Title = "Cấp độ trung bình của phụ kiện xuất hiện",
                Value = cfg.avgArtifactLevel,
            });

            statLineCost.Bind(new TransmutationSystemTotalStatEntry
            {
                IsUnique = false,
                Title = "Chi phí dung hợp",
                Value = cfg.cost,
            });
        }

        private void RefreshVisual()
        {
            var cfg = _rows[_currentIdx];

            var dic = new Dictionary<EEquipmentTier, float>
            {
                { EEquipmentTier.D, cfg.d },
                { EEquipmentTier.C, cfg.c },
                { EEquipmentTier.B, cfg.b },
                { EEquipmentTier.A, cfg.a },
                { EEquipmentTier.S, cfg.s },
                { EEquipmentTier.SS, cfg.sS },
                { EEquipmentTier.SSS, cfg.sSS },
                { EEquipmentTier.R, cfg.r },
                { EEquipmentTier.SR, cfg.sR },
            };

            for (var i = 0; i < dic.Count; i++)
            {
                var item = dic.ElementAt(i);
                var cfgTier = DatabaseManager.Instance.EquipmentTierDatabase.Get(item.Key);

                if (_levelInfos.Count > i)
                {
                    var clone = _levelInfos[i];
                    clone.gameObject.SetActive(true);
                    clone.transform.SetParent(levelInfoContainer);
                    clone.Bind(cfgTier.tier, item.Value);
                }
                else
                {
                    var clone = Instantiate(levelInfoPrefab, levelInfoContainer);
                    clone.Bind(cfgTier.tier, item.Value);
                    _levelInfos.Add(clone);
                }
            }
        }
    }
}