using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Modules;
using Immortal_Switch.Scripts.PlayerSystem.Views.UI;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.PlayerSystem.Views
{
    [Serializable]
    public class ProfileFinalStatItem
    {
        public StatType stat;
        public UIProfileRowOption option;
    }

    public class ProfileView : AnimatedUIView
    {
        [Header("References")]
        [SerializeField]
        private TMP_Text txtName;

        [SerializeField]
        private TMP_Text txtPower;

        [SerializeField]
        private Button btnRename;

        [SerializeField]
        private Button btnClose;

        [SerializeField]
        private UIProfileRenamePopup uiRename;

        [Header("Options")]
        [SerializeField]
        private RectTransform optionContainer;

        [SerializeField]
        private UIProfileTitleOption optionTitleFinal;

        [SerializeField]
        private List<ProfileFinalStatItem> finalStats = new();

        [SerializeField]
        private UIProfileTitleOption optionTitleAll;

        [SerializeField]
        private UIProfileRowOption optionRowTemplate;

        // --- Private Fields ---
        private List<DynamicHeroesGlobalSpecificationsConfigStatsInfoRow> _configStats;
        private SimpleUIPool<UIProfileRowOption> _pools;

        private void Awake()
        {
            btnRename.onClick.AddListener(OnClickRename);
            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnDestroy()
        {
            btnRename.onClick.RemoveListener(OnClickRename);
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickRename()
        {
            uiRename.gameObject.SetActive(true);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<ProfileView>().Forget();
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            _configStats = DatabaseManager.Instance.GetConfigStats();

            RefreshVisual();
            RefreshFinalStats();
            RefreshOtherStats();
        }

        public void RefreshVisual()
        {
            var playerCp = ModuleManager.Instance.PowerService.CalculatePlayerCp();

            txtName.text = UserDataCache.Instance.DisplayName;
            txtPower.text = BigNumber.FromDouble(playerCp).ToInputString();
        }

        private void RefreshFinalStats()
        {
            foreach (var stat in finalStats)
            {
                var cfg = _configStats.FirstOrDefault(v => v.isBase && v.statType == (int)stat.stat);

                if (cfg == null)
                {
                    continue;
                }

                var statValue = PowerUpManager.Instance.GetFlatValue(stat.stat);

                stat.option.Bind(cfg.uiKey, statValue.ToString("N0"));
            }
        }

        private void RefreshOtherStats()
        {
            _pools ??= new SimpleUIPool<UIProfileRowOption>(optionRowTemplate, optionContainer);

            var otherStats = _configStats
                .Where(v => !v.isBase)
                .ToList();

            for (int i = 0; i < otherStats.Count; i++)
            {
                var stat = otherStats[i];
                var statType = (StatType)stat.statType;

                var statValue = stat.valueType == (int)ModifierOp.Multiply
                    ? PowerUpManager.Instance.GetPercentOfBaseValue(statType)
                    : PowerUpManager.Instance.GetFlatValue(statType);

                var clone = _pools.Get(i);

                clone.Bind(stat.uiKey, statValue.ToString("N0"));
            }
        }
    }
}