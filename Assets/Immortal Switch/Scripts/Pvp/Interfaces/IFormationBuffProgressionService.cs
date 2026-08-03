using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Formation Buff Progression (upgrade) service (DOCX §22, §28). Phase-1 = Local (atomic);
    /// Phase-2 server. Upgrade validate ownership/level/shards/token/max, deduct atomically, save.
    /// </summary>
    public interface IFormationBuffProgressionService
    {
        UniTask<FormationBuffUpgradePreview> PreviewUpgradeAsync(string buffId, CancellationToken token);

        UniTask<FormationBuffUpgradeResult> UpgradeAsync(string transactionId, string buffId, CancellationToken token);
    }
}
