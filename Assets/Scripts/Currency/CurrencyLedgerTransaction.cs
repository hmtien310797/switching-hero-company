using System;
using Immortal_Switch.Scripts.Core;

namespace Immortal_Switch.Scripts.Currency
{
    [Serializable]
    public class CurrencyLedgerTransaction
    {
        public string TransactionId;
        public CurrencyTransactionType Type;
        public CurrencyTransactionReason Reason;
        public CurrencyType CurrencyType;
        public BigNumber Amount;

        // Optional metadata để sau này gửi server biết user mua/nâng cấp cái gì.
        public string PayloadJson;

        public long CreatedUnixTime;
        public bool IsSynced;
    }
}