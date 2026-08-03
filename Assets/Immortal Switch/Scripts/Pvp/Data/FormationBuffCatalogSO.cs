using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Database SO chứa toàn bộ <see cref="FormationBuffDataSO"/>. Đặt ở
    /// <c>Assets/Resources/PvP/FormationBuffCatalog.asset</c> để
    /// <see cref="Immortal_Switch.Scripts.Pvp.Services.LocalFormationBuffCatalogService"/>
    /// tự <c>Resources.Load</c>.
    /// <para><b>FLAGGED:</b> asset phải được tạo trong editor; nếu thiếu, service fallback default
    /// catalog từ <c>PvpTestBuffIds</c> để Phase-1 vẫn testable.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "PvP/Formation Buff Catalog")]
    public sealed class FormationBuffCatalogSO : ScriptableObject
    {
        public FormationBuffDataSO[] Buffs = Array.Empty<FormationBuffDataSO>();
    }
}
