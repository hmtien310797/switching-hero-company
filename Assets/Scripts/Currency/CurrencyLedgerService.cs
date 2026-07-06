using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyLedgerService : MonoBehaviour
    {
        public static CurrencyLedgerService Instance { get; private set; }

        [Header("Sync")]
        [SerializeField] private int autoSyncIntervalSeconds = 60;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private readonly List<CurrencyLedgerTransaction> pendingTransactions = new List<CurrencyLedgerTransaction>();

        private float syncTimer;
        private bool isSyncing;
        public event Action<CurrencyLedgerChangedArgs> OnCurrencyLedgerChanged;
        public event Action OnAnyLedgerChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // CurrencyManager.Set() (full-balance sync from server: login, summon result, battle
        // reward, ...) only fires CurrencyManager.OnCurrencyChanged. UI (CurrencyTextBinder) only
        // listens to OnCurrencyLedgerChanged below, so without this bridge a server-driven Set()
        // never reaches the UI — it only happened to look fine before this when some other
        // ledger transaction (income/spend) for the same currency type fired separately.
        private void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyManagerChanged;
            }
        }

        private void HandleCurrencyManagerChanged(CurrencyChangedArgs args)
        {
            NotifyLedgerChanged(args.CurrencyType);
        }

        private void Update()
        {
            syncTimer += Time.deltaTime;

            if (syncTimer >= autoSyncIntervalSeconds)
            {
                syncTimer = 0f;
                SyncPendingTransactions().Forget();
            }
        }

        public BigNumber GetConfirmedBalance(CurrencyType currencyType)
        {
            return CurrencyManager.Instance != null
                ? CurrencyManager.Instance.Get(currencyType)
                : BigNumber.Zero;
        }
        
        public void AddOrMergeIncome(
            CurrencyType currencyType,
            BigNumber amount,
            CurrencyTransactionReason reason,
            string payloadJson = null
        )
        {
            if (amount <= BigNumber.Zero)
                return;

            for (int i = pendingTransactions.Count - 1; i >= 0; i--)
            {
                CurrencyLedgerTransaction tx = pendingTransactions[i];

                if (tx.IsSynced)
                    continue;

                if (tx.Type != CurrencyTransactionType.Income)
                    continue;

                if (tx.CurrencyType != currencyType)
                    continue;

                if (tx.Reason != reason)
                    continue;

                if (tx.PayloadJson != payloadJson)
                    continue;

                tx.Amount += amount;

                if (enableDebugLog)
                {
                    Debug.Log($"[CurrencyLedger] MergeIncome {currencyType} +{amount.ToInputString()} reason={reason}");
                }
                
                NotifyLedgerChanged(currencyType);
                return;
            }

            AddIncome(currencyType, amount, reason, payloadJson);
        }

        public BigNumber GetPendingDelta(CurrencyType currencyType)
        {
            BigNumber result = BigNumber.Zero;

            for (int i = 0; i < pendingTransactions.Count; i++)
            {
                CurrencyLedgerTransaction tx = pendingTransactions[i];

                if (tx.IsSynced)
                    continue;

                if (tx.CurrencyType != currencyType)
                    continue;

                if (tx.Type == CurrencyTransactionType.Income)
                    result += tx.Amount;
                else if (tx.Type == CurrencyTransactionType.Spend)
                    result -= tx.Amount;
            }

            return result;
        }

        public BigNumber GetDisplayBalance(CurrencyType currencyType)
        {
            return BigNumber.Max(
                BigNumber.Zero,
                GetConfirmedBalance(currencyType) + GetPendingDelta(currencyType)
            );
        }

        public bool HasEnoughDisplayBalance(CurrencyType currencyType, BigNumber cost)
        {
            if (cost <= BigNumber.Zero)
                return true;

            return GetDisplayBalance(currencyType) >= cost;
        }

        private void AddIncome(
            CurrencyType currencyType,
            BigNumber amount,
            CurrencyTransactionReason reason,
            string payloadJson = null
        )
        {
            if (amount <= BigNumber.Zero)
                return;

            CurrencyLedgerTransaction tx = CreateTransaction(
                CurrencyTransactionType.Income,
                reason,
                currencyType,
                amount,
                payloadJson
            );

            pendingTransactions.Add(tx);

            if (enableDebugLog)
            {
                Debug.Log($"[CurrencyLedger] AddIncome {currencyType} +{amount.ToInputString()} reason={reason}");
            }
            
            NotifyLedgerChanged(currencyType);
        }

        public bool TrySpend(
            CurrencyType currencyType,
            BigNumber cost,
            CurrencyTransactionReason reason,
            string payloadJson = null
        )
        {
            if (cost <= BigNumber.Zero)
                return true;

            if (!HasEnoughDisplayBalance(currencyType, cost))
            {
                if (enableDebugLog)
                {
                    Debug.Log(
                        $"[CurrencyLedger] Not enough display balance. " +
                        $"Currency={currencyType}, Cost={cost.ToInputString()}, Display={GetDisplayBalance(currencyType).ToInputString()}"
                    );
                }

                return false;
            }

            CurrencyLedgerTransaction tx = CreateTransaction(
                CurrencyTransactionType.Spend,
                reason,
                currencyType,
                cost,
                payloadJson
            );

            pendingTransactions.Add(tx);

            if (enableDebugLog)
            {
                Debug.Log($"[CurrencyLedger] Spend {currencyType} -{cost.ToInputString()} reason={reason}");
            }
            
            NotifyLedgerChanged(currencyType);
            return true;
        }

        public async UniTask<bool> SyncPendingTransactions()
        {
            if (isSyncing)
                return false;

            if (pendingTransactions.Count == 0)
                return true;

            List<CurrencyLedgerTransaction> unsynced = GetUnsyncedTransactions();

            if (unsynced.Count == 0)
                return true;

            isSyncing = true;

            CurrencyLedgerSyncRequest request = BuildSyncRequest(unsynced);

            if (enableDebugLog)
            {
                Debug.Log($"[CurrencyLedger] Sync batch={request.batchId}, count={request.transactions.Count}");
            }

            // TODO SERVER:
            // Đây là sync tạm local demo.
            // Sau này thay bằng:
            // var response = await serverApi.SyncCurrencyLedger(request);
            // if (response.success) CurrencyManager.Instance.ApplyServerBalances(response.balances);
            //
            // Server nên xử lý idempotent theo transactionId/batchId để tránh cộng/trừ trùng.
            ApplyTransactionsLocallyForDemo(unsynced);

            MarkTransactionsSynced(unsynced);
            RemoveSyncedTransactions();

            isSyncing = false;
            NotifyLedgerChangedForTransactions(unsynced);

            await UniTask.CompletedTask;
            return true;
        }
        
        public List<CurrencyLedgerTransaction> GetPendingTransactionsSnapshot()
        {
            return new List<CurrencyLedgerTransaction>(pendingTransactions);
        }
        
        public IReadOnlyList<CurrencyLedgerTransaction> GetPendingTransactions()
        {
            return pendingTransactions;
        }

        public int GetPendingTransactionCount()
        {
            return pendingTransactions.Count;
        }

        /// <summary>
        /// Xoá tất cả pending transaction chưa sync của một reason cụ thể.
        /// Dùng trước khi áp dụng server balance để tránh double-count.
        /// </summary>
        public void ClearPendingByReason(CurrencyTransactionReason reason)
        {
            HashSet<CurrencyType> cleared = new HashSet<CurrencyType>();

            for (int i = pendingTransactions.Count - 1; i >= 0; i--)
            {
                CurrencyLedgerTransaction tx = pendingTransactions[i];
                if (!tx.IsSynced && tx.Reason == reason)
                {
                    cleared.Add(tx.CurrencyType);
                    pendingTransactions.RemoveAt(i);
                }
            }

            foreach (CurrencyType type in cleared)
                NotifyLedgerChanged(type);
        }

        private CurrencyLedgerTransaction CreateTransaction(
            CurrencyTransactionType type,
            CurrencyTransactionReason reason,
            CurrencyType currencyType,
            BigNumber amount,
            string payloadJson
        )
        {
            return new CurrencyLedgerTransaction
            {
                TransactionId = GenerateTransactionId(),
                Type = type,
                Reason = reason,
                CurrencyType = currencyType,
                Amount = amount,
                PayloadJson = payloadJson,
                CreatedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsSynced = false
            };
        }

        private List<CurrencyLedgerTransaction> GetUnsyncedTransactions()
        {
            List<CurrencyLedgerTransaction> result = new List<CurrencyLedgerTransaction>();

            for (int i = 0; i < pendingTransactions.Count; i++)
            {
                if (!pendingTransactions[i].IsSynced)
                    result.Add(pendingTransactions[i]);
            }

            return result;
        }

        private CurrencyLedgerSyncRequest BuildSyncRequest(List<CurrencyLedgerTransaction> transactions)
        {
            CurrencyLedgerSyncRequest request = new CurrencyLedgerSyncRequest
            {
                batchId = GenerateBatchId(),
                transactions = new List<CurrencyLedgerTransactionDto>()
            };

            for (int i = 0; i < transactions.Count; i++)
            {
                CurrencyLedgerTransaction tx = transactions[i];

                request.transactions.Add(new CurrencyLedgerTransactionDto
                {
                    transactionId = tx.TransactionId,
                    type = tx.Type.ToString(),
                    reason = tx.Reason.ToString(),
                    currencyType = tx.CurrencyType.ToString(),
                    amount = tx.Amount.ToString(),
                    payloadJson = tx.PayloadJson,
                    createdUnixTime = tx.CreatedUnixTime
                });
            }

            return request;
        }

        private void ApplyTransactionsLocallyForDemo(List<CurrencyLedgerTransaction> transactions)
        {
            if (CurrencyManager.Instance == null)
                return;

            for (int i = 0; i < transactions.Count; i++)
            {
                CurrencyLedgerTransaction tx = transactions[i];

                // TODO SERVER:
                // Đây chỉ là apply local demo.
                // Server mode sẽ không Add/Spend local kiểu này.
                if (tx.Type == CurrencyTransactionType.Income)
                {
                    CurrencyManager.Instance.AddLocalDemo(tx.CurrencyType, tx.Amount);
                }
                else if (tx.Type == CurrencyTransactionType.Spend)
                {
                    CurrencyManager.Instance.SpendLocalDemo(tx.CurrencyType, tx.Amount);
                }
            }
        }

        private void MarkTransactionsSynced(List<CurrencyLedgerTransaction> transactions)
        {
            for (int i = 0; i < transactions.Count; i++)
            {
                transactions[i].IsSynced = true;
            }
        }

        private void RemoveSyncedTransactions()
        {
            for (int i = pendingTransactions.Count - 1; i >= 0; i--)
            {
                if (pendingTransactions[i].IsSynced)
                    pendingTransactions.RemoveAt(i);
            }
        }

        private static string GenerateTransactionId()
        {
            return $"tx_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
        }

        private static string GenerateBatchId()
        {
            return $"batch_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
        }
        
        private void NotifyLedgerChanged(CurrencyType currencyType)
        {
            OnCurrencyLedgerChanged?.Invoke(new CurrencyLedgerChangedArgs
            {
                CurrencyType = currencyType,
                ConfirmedBalance = GetConfirmedBalance(currencyType),
                PendingDelta = GetPendingDelta(currencyType),
                DisplayBalance = GetDisplayBalance(currencyType)
            });

            OnAnyLedgerChanged?.Invoke();
        }
        
        private void NotifyLedgerChangedForTransactions(List<CurrencyLedgerTransaction> transactions)
        {
            HashSet<CurrencyType> changedTypes = new HashSet<CurrencyType>();

            for (int i = 0; i < transactions.Count; i++)
            {
                changedTypes.Add(transactions[i].CurrencyType);
            }

            foreach (CurrencyType type in changedTypes)
            {
                NotifyLedgerChanged(type);
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyManagerChanged;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                SyncPendingTransactions().Forget();
        }

        private void OnApplicationQuit()
        {
            SyncPendingTransactions().Forget();
        }
    }
    
    public class CurrencyLedgerChangedArgs
    {
        public CurrencyType CurrencyType;
        public BigNumber ConfirmedBalance;
        public BigNumber PendingDelta;
        public BigNumber DisplayBalance;
    }
}