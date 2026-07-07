using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Currency
{
    [Serializable]
    public class CurrencyLedgerSyncRequest
    {
        public string batchId;
        public List<CurrencyLedgerTransactionDto> transactions;
    }

    [Serializable]
    public class CurrencyLedgerTransactionDto
    {
        public string transactionId;
        public string type;
        public string reason;
        public string currencyType;
        public string amount;
        public string payloadJson;
        public long createdUnixTime;
    }

    [Serializable]
    public class CurrencyLedgerSyncResponse
    {
        public bool success;
        public string error;

        // Server trả balances cuối cùng.
        public List<CurrencyBalanceDto> balances;
    }

    [Serializable]
    public class CurrencyBalanceDto
    {
        public string currencyType;
        public string amount;
    }
}