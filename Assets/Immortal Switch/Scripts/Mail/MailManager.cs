using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Mail
{
    /// <summary>
    /// Cache trạng thái Mail lấy từ server (RPC mail/*) — server là nguồn sự thật duy nhất.
    /// </summary>
    public class MailManager : Singleton<MailManager>
    {
        /// <summary>Phát ra khi danh sách thư được refresh từ server.</summary>
        public event Action OnDataChanged;

        /// <summary>Snapshot danh sách thư gần nhất lấy từ mail/list (mới nhất trước).</summary>
        public List<MailDto> Mails { get; private set; } = new();

        public override UniTask InitializeAsync() => UniTask.CompletedTask;

        /// <summary>Tải lại toàn bộ danh sách thư từ server. Gọi khi mở MailView và sau mỗi
        /// claim/claim_all thành công để đồng bộ lại.</summary>
        public async UniTask RefreshAsync()
        {
            try
            {
                var response = await NakamaClient.Instance.MailListAsync();
                Mails = response.Mails ?? new List<MailDto>();
                OnDataChanged?.Invoke();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[MailManager] mail/list failed: {ex.StatusCode} {ex.Message}");
            }
        }

        /// <summary>Nhận thưởng 1 thư và refresh lại danh sách. Trả reward cho UI hiển thị popup.</summary>
        public async UniTask<(List<MailItemDto> rewards, string error)> ClaimAsync(string mailId)
        {
            MailClaimResponse response;

            try
            {
                response = await NakamaClient.Instance.MailClaimAsync(new MailClaimRequest { MailId = mailId });
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[MailManager] mail/claim error {ex.StatusCode}: {ex.Message}");
                return (new List<MailItemDto>(), "NETWORK_ERROR");
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return (response.Rewards ?? new List<MailItemDto>(), null);
        }

        /// <summary>Nhận toàn bộ thư chưa nhận và refresh lại danh sách. Trả tổng reward cho UI
        /// hiển thị popup.</summary>
        public async UniTask<(int claimedCount, List<MailItemDto> rewards, string error)> ClaimAllAsync()
        {
            MailClaimAllResponse response;

            try
            {
                response = await NakamaClient.Instance.MailClaimAllAsync();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[MailManager] mail/claim_all error {ex.StatusCode}: {ex.Message}");
                return (0, new List<MailItemDto>(), "NETWORK_ERROR");
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return (response.ClaimedCount, response.Rewards ?? new List<MailItemDto>(), null);
        }
    }
}
