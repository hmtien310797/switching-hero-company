using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventFishing.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing.Views
{
    public class EventFishingCollectionView : AnimatedUIView
    {
        [SerializeField]
        private Button btnClose;

        [Header("Progress references")]
        [SerializeField]
        private Button btnProgress;

        [SerializeField]
        private GameObject goProgress;

        [SerializeField]
        private TextMeshProUGUI txtProgress1;

        [SerializeField]
        private TextMeshProUGUI txtProgress2;

        [SerializeField]
        private Image imgFill;

        [Header("Collection references")]
        [SerializeField]
        private RectTransform collectionContainer;

        [SerializeField]
        private UIEventFishingCollectionItem collectionPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIEventFishingCollectionItem> _pool;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnProgress.onClick.AddListener(OnClickProgress);
            OnClickProgress();
        }

        private void OnClickProgress()
        {
            goProgress.SetActive(!goProgress.activeInHierarchy);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            Bind();
        }

        private void OnDestroy()
        {
            btnClose.onClick.RemoveListener(OnClickClose);
            btnProgress.onClick.RemoveListener(OnClickProgress);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<EventFishingCollectionView>();
        }

        private void Bind()
        {
            var collections = DatabaseManager.Instance.GetEventFishingCollection();
            var caughtCollectionCount = EventFishingManager.Instance.CaughtCount;

            txtProgress1.text = $"{caughtCollectionCount}/{collections.Count}";
            txtProgress2.text = $"{caughtCollectionCount}/{collections.Count}";
            imgFill.fillAmount = caughtCollectionCount / (float)collections.Count;

            RefreshCollection(collections);
        }

        private void RefreshCollection(List<DynamicHeroesGlobalSpecificationsEventFishingCollectionRow> rows)
        {
            _pool ??= new SimpleUIPool<UIEventFishingCollectionItem>(collectionPrefab, collectionContainer);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var clone = _pool.Get(i);
                var isClaimed = EventFishingManager.Instance.HasCaughtFish(row.collectionId);

                clone.Bind(row.collectionId, isClaimed);
            }

            _pool.ReleaseFrom(rows.Count);
        }
    }
}