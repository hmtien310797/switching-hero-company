using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Data;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Catalog service cho Formation Buff (DOCX §22). Views/Controllers gọi interface; không đọc
    /// SO hay ES3 trực tiếp. Phase-1 = LocalFormationBuffCatalogService; Phase-2 server có thể
    /// thay bằng remote catalog mà không sửa UI/combat.
    /// </summary>
    public interface IFormationBuffCatalogService
    {
        IReadOnlyList<FormationBuffDataSO> GetAll();

        /// <summary>Trả buff theo BuffId; null + log error nếu thiếu (DOCX §22 "GetRequired").</summary>
        FormationBuffDataSO GetRequired(string buffId);

        bool TryGet(string buffId, out FormationBuffDataSO data);
    }
}
