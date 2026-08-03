using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Formation service (DOCX §5). Validate + save/load pvp_formation. UI gọi interface, không
    /// gọi ES3. Validation rules (DOCX §17): Front != Back; slot-type matching; FrontOnly/BackOnly
    /// compat; group conflict; one BuffId only on one position.
    /// </summary>
    public interface IPvPFormationService
    {
        PvpFormationValidationResult Validate(PvpFormationSaveData formation);

        UniTask SaveFormationAsync(PvpFormationSaveData formation, CancellationToken token);

        /// <summary>Đọc formation hiện tại từ repository (FLAGGED: convenience — không có trong
        /// DOCX §5 nhưng UI cần load để bind).</summary>
        PvpFormationSaveData LoadFormation();
    }
}
