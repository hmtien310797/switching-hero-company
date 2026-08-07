using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Bảng tier/rank reward PvP (DOCX §32 — rank/reward config tách riêng để server thay sau).
    /// Nguồn dữ liệu là Google Sheet (importer: PvpTierRewardGoogleSheetImporterWindow). Load qua
    /// DatabaseManager.PvpTierRewardDatabase ([DatabaseBinding] — addressable label "game_database").
    /// <para>TODO server: khi server trả tier config thật thay cho bảng local này.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "PvpTierRewardDatabase", menuName = "PvP/Tier Reward Database")]
    public sealed class PvpTierRewardDatabaseSO : ScriptableObject
    {
        public List<PvpTierRewardRow> Rows = new();

        /// <summary>Row khớp đúng tier_id.</summary>
        public PvpTierRewardRow GetRowById(int tierId)
        {
            if (Rows == null) return null;
            for (int i = 0; i < Rows.Count; i++)
                if (Rows[i] != null && Rows[i].TierId == tierId) return Rows[i];
            return null;
        }

        /// <summary>Row có RequiredPoint cao nhất mà &lt;= point (tier hiện tại theo rank point).</summary>
        public PvpTierRewardRow GetRowByPoint(int point)
        {
            if (Rows == null) return null;
            PvpTierRewardRow best = null;
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                if (r == null) continue;
                if (r.RequiredPoint <= point && (best == null || r.RequiredPoint > best.RequiredPoint))
                    best = r;
            }
            return best;
        }

        /// <summary>Row có RequiredPoint cao nhất.</summary>
        public PvpTierRewardRow GetHighestRow()
        {
            if (Rows == null) return null;
            PvpTierRewardRow best = null;
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                if (r == null) continue;
                if (best == null || r.RequiredPoint > best.RequiredPoint)
                    best = r;
            }
            return best;
        }
    }

    [Serializable]
    public sealed class PvpTierRewardRow
    {
        public int TierId;          // 1..25
        public string TierName;     // bronze..diamond
        public int TierLevel;       // 1..5
        public int RequiredPoint;   // rank threshold
        public string Reward;       // "3:1000;1:5000" (itemId:qty;itemId:qty;...)
    }
}