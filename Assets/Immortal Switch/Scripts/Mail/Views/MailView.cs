using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Mail.Views.UI;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Mail.Views
{
    public class MailView : AnimatedUIView
    {
        [SerializeField]
        private Button btnClaimAll;

        [Header("Mail references")]
        [SerializeField]
        private RectTransform mailContainer;

        [SerializeField]
        private UIMailItem mailPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIMailItem> _pool;

        private void Awake()
        {
            btnClaimAll.onClick.AddListener(OnClickClaimAll);
        }

        private void OnDestroy()
        {
            btnClaimAll.onClick.RemoveListener(OnClickClaimAll);
        }

        public override async UniTask PlayShowAsync(object args)
        {
            await MailManager.Instance.RefreshAsync();
            RefreshMails();
            await base.PlayShowAsync(args);
        }

        private void RefreshMails()
        {
            _pool ??= new SimpleUIPool<UIMailItem>(mailPrefab, mailContainer);

            var mails = MailManager.Instance.Mails;
            var hasClaim = false;

            for (int i = 0; i < mails.Count; i++)
            {
                var mail = mails[i];
                var rewards = ToItemData(mail.Rewards);
                var clone = _pool.Get(i);

                if (!mail.Claimed)
                {
                    hasClaim = true;
                }

                clone.Bind(mail.MailId, mail.Title, mail.Content, rewards, mail.Claimed, OnClickClaimMail);
            }

            _pool.ReleaseFrom(mails.Count);
            btnClaimAll.gameObject.SetActive(hasClaim);
        }

        private static List<ItemData> ToItemData(List<MailItemDto> rewards)
        {
            return rewards?.Select(r => new ItemData(r.ItemId, r.Amount)).ToList() ?? new List<ItemData>();
        }

        private async void OnClickClaimAll()
        {
            btnClaimAll.interactable = false;
            var (claimedCount, rewards, error) = await MailManager.Instance.ClaimAllAsync();
            btnClaimAll.interactable = true;

            if (error != null)
            {
                Debug.LogError($"[MailView] mail/claim_all failed: {error}");
                return;
            }

            RefreshMails();

            if (claimedCount > 0)
            {
                PopupRewardService.Show(ToItemData(rewards));
            }
        }

        private async void OnClickClaimMail(string mailId)
        {
            var (rewards, error) = await MailManager.Instance.ClaimAsync(mailId);

            if (error != null)
            {
                Debug.LogError($"[MailView] mail/claim failed for {mailId}: {error}");
                return;
            }

            RefreshMails();

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(ToItemData(rewards));
            }
        }
    }
}