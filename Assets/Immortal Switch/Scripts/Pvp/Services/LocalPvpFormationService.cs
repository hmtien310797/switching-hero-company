using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 formation service (DOCX §5, §17). Validate + save/load pvp_formation qua repository.
    /// Validation: Front != Back; slot-type matching; FrontOnly/BackOnly compat; group conflict;
    /// one BuffId only on one position. UI/Controller gọi interface, không đụng ES3.
    /// </summary>
    internal sealed class LocalPvpFormationService : IPvPFormationService
    {
        private readonly IPvPRepository _repo;
        private readonly IFormationBuffCatalogService _catalog;

        public LocalPvpFormationService(IPvPRepository repo, IFormationBuffCatalogService catalog)
        {
            _repo = repo;
            _catalog = catalog;
        }

        public PvpFormationSaveData LoadFormation()
        {
            return _repo.Load<PvpFormationSaveData>(PvpEs3Keys.Formation);
        }

        public UniTask SaveFormationAsync(PvpFormationSaveData formation, CancellationToken token)
        {
            var result = Validate(formation);
            if (!result.IsValid)
                throw new System.InvalidOperationException($"Cannot save invalid formation: {result}");

            _repo.Save(PvpEs3Keys.Formation, formation);
            return UniTask.CompletedTask;
        }

        public PvpFormationValidationResult Validate(PvpFormationSaveData formation)
        {
            var result = new PvpFormationValidationResult { IsValid = true };
            if (formation == null)
            {
                result.Errors.Add("Formation is null.");
                result.IsValid = false;
                return result;
            }

            // Front != Back hero (DOCX §17).
            if (formation.FrontHeroId > 0 && formation.FrontHeroId == formation.BackHeroId)
                result.Errors.Add("Front and Back heroes must be different (DOCX §17).");

            ValidateLoadout(formation.FrontLoadout, FormationSlot.Front, result);
            ValidateLoadout(formation.BackLoadout, FormationSlot.Back, result);
            CheckNoDuplicateBuffAcrossSlots(formation, result);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private void ValidateLoadout(PvpBuffSlotLoadout loadout, FormationSlot slot,
            PvpFormationValidationResult result)
        {
            if (loadout == null) return;

            // GroupId → slot đã thấy, để phát hiện group conflict trong cùng loadout.
            var seenGroups = new Dictionary<string, BuffSlotType>();

            ValidateOneSlot(loadout.Core, BuffSlotType.Core, slot, result, seenGroups);
            ValidateOneSlot(loadout.Support, BuffSlotType.Support, slot, result, seenGroups);
            ValidateOneSlot(loadout.Trigger, BuffSlotType.Trigger, slot, result, seenGroups);
        }

        private void ValidateOneSlot(string buffId, BuffSlotType slotType, FormationSlot formationSlot,
            PvpFormationValidationResult result, Dictionary<string, BuffSlotType> seenGroups)
        {
            if (string.IsNullOrEmpty(buffId)) return;

            if (!_catalog.TryGet(buffId, out var buff))
            {
                result.Errors.Add($"Unknown BuffId '{buffId}' in {formationSlot}/{slotType} slot.");
                return;
            }

            // Slot-type matching: buff phải nằm đúng slot type của nó.
            if (buff.SlotType != slotType)
            {
                result.Errors.Add(
                    $"Buff '{buffId}' is a {buff.SlotType} buff but placed in {slotType} slot ({formationSlot}).");
            }

            // Formation-slot compatibility (DOCX §17 — FrontOnly/BackOnly).
            switch (buff.FormationSlotCompat)
            {
                case FormationBuffSlotCompat.FrontOnly when formationSlot == FormationSlot.Back:
                    result.Errors.Add($"Buff '{buffId}' is FrontOnly and cannot be equipped on Back (DOCX §17).");
                    break;
                case FormationBuffSlotCompat.BackOnly when formationSlot == FormationSlot.Front:
                    result.Errors.Add($"Buff '{buffId}' is BackOnly and cannot be equipped on Front (DOCX §17).");
                    break;
            }

            // Group conflict trong cùng loadout.
            if (!string.IsNullOrEmpty(buff.GroupId))
            {
                if (seenGroups.ContainsKey(buff.GroupId))
                {
                    result.Errors.Add(
                        $"Group conflict: Buff '{buffId}' shares GroupId '{buff.GroupId}' with another buff in {formationSlot} loadout (DOCX §17).");
                }
                else
                {
                    seenGroups[buff.GroupId] = slotType;
                }
            }
        }

        private void CheckNoDuplicateBuffAcrossSlots(PvpFormationSaveData formation,
            PvpFormationValidationResult result)
        {
            // BuffId → vị trí đầu tiên thấy. One BuffId only on one position (DOCX §17).
            var seen = new Dictionary<string, string>();
            CheckOne(formation.FrontLoadout, "Front", seen, result);
            CheckOne(formation.BackLoadout, "Back", seen, result);
        }

        private void CheckOne(PvpBuffSlotLoadout loadout, string label,
            Dictionary<string, string> seen, PvpFormationValidationResult result)
        {
            if (loadout == null) return;
            TryAddSeen(loadout.Core, label + "/Core", seen, result);
            TryAddSeen(loadout.Support, label + "/Support", seen, result);
            TryAddSeen(loadout.Trigger, label + "/Trigger", seen, result);
        }

        private void TryAddSeen(string buffId, string location,
            Dictionary<string, string> seen, PvpFormationValidationResult result)
        {
            if (string.IsNullOrEmpty(buffId)) return;
            if (seen.TryGetValue(buffId, out var first))
            {
                result.Errors.Add(
                    $"BuffId '{buffId}' is equipped in both {first} and {location}; one BuffId may only be equipped on one position (DOCX §17).");
            }
            else
            {
                seen[buffId] = location;
            }
        }
    }
}
