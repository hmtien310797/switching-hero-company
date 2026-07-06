using System.Collections.Generic;
using System.Text;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.DebugTools
{
    public class CurrencyLedgerDebugPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text contentText;

        [Header("Config")]
        [SerializeField] private CurrencyType[] watchCurrencies =
        {
            CurrencyType.gold,
            CurrencyType.diamond
        };

        [SerializeField] private bool showOnStart;
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;

        private readonly StringBuilder builder = new StringBuilder(2048);

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            root.SetActive(showOnStart);
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }
        }

        private void Subscribe()
        {
            if (CurrencyLedgerService.Instance != null)
            {
                CurrencyLedgerService.Instance.OnAnyLedgerChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (CurrencyLedgerService.Instance != null)
            {
                CurrencyLedgerService.Instance.OnAnyLedgerChanged -= Refresh;
            }
        }

        public void Toggle()
        {
            if (root == null)
                return;

            root.SetActive(!root.activeSelf);

            if (root.activeSelf)
                Refresh();
        }

        public void Refresh()
        {
            if (contentText == null)
                return;

            if (!root.activeSelf)
                return;

            CurrencyLedgerService ledger = CurrencyLedgerService.Instance;

            if (ledger == null)
            {
                contentText.text = "[CurrencyLedgerDebug]\nMissing CurrencyLedgerService.";
                return;
            }

            builder.Clear();

            builder.AppendLine("<b>Currency Ledger Debug</b>");
            builder.AppendLine();

            AppendBalances(ledger);
            AppendTransactions(ledger);

            contentText.text = builder.ToString();
        }

        private void AppendBalances(CurrencyLedgerService ledger)
        {
            builder.AppendLine("<b>Balances</b>");

            for (int i = 0; i < watchCurrencies.Length; i++)
            {
                CurrencyType type = watchCurrencies[i];

                BigNumber confirmed = ledger.GetConfirmedBalance(type);
                BigNumber pending = ledger.GetPendingDelta(type);
                BigNumber display = ledger.GetDisplayBalance(type);

                builder.Append(type);
                builder.AppendLine(":");

                builder.Append("  Confirmed: ");
                builder.AppendLine(Format(confirmed));

                builder.Append("  Pending:   ");
                builder.AppendLine(FormatSigned(pending));

                builder.Append("  Display:   ");
                builder.AppendLine(Format(display));

                builder.AppendLine();
            }
        }

        private void AppendTransactions(CurrencyLedgerService ledger)
        {
            List<CurrencyLedgerTransaction> transactions = ledger.GetPendingTransactionsSnapshot();

            builder.Append("<b>Pending Transactions</b> ");
            builder.Append("(");
            builder.Append(transactions.Count);
            builder.AppendLine(")");

            if (transactions.Count == 0)
            {
                builder.AppendLine("  None");
                return;
            }

            for (int i = 0; i < transactions.Count; i++)
            {
                CurrencyLedgerTransaction tx = transactions[i];

                builder.Append(i + 1);
                builder.Append(". ");

                builder.Append(tx.Type == CurrencyTransactionType.Income ? "+" : "-");
                builder.Append(Format(tx.Amount));

                builder.Append(" ");
                builder.Append(tx.CurrencyType);

                builder.Append(" | ");
                builder.Append(tx.Reason);

                if (tx.IsSynced)
                    builder.Append(" | Synced");

                if (!string.IsNullOrWhiteSpace(tx.PayloadJson))
                {
                    builder.Append(" | ");
                    builder.Append(tx.PayloadJson);
                }

                builder.AppendLine();
            }
        }

        private string Format(BigNumber value)
        {
            return value.ToInputString();
        }

        private string FormatSigned(BigNumber value)
        {
            if (value > BigNumber.Zero)
                return "+" + Format(value);

            return Format(value);
        }
    }
}