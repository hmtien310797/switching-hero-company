using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem
{
    internal class MissionSystemService : IMissionSystemService
    {
        private readonly MissionSystemDatabaseSO _db;
        private readonly IMissionSystemStorage _storage;

        public MissionSystemService(IMissionSystemStorage storage, MissionSystemDatabaseSO db)
        {
            _storage = storage;
            _db = db;
        }

        public bool UpdateProgress(EMissionSystemType type, int progress)
        {
            var entry = _db.GetEntry(_storage.Data.Id);

            if (entry != null &&
                entry.Value.type == type)
            {
                _storage.Data.SetProgress(progress);
                _storage.Save();
                return true;
            }

            return false;
        }

        public bool IsComplete()
        {
            var entry = _db.GetEntry(_storage.Data.Id);
            return entry != null && entry.Value.target == _storage.Data.Progress;
        }

        public void Complete()
        {
            var nextEntry = _db.NextEntry(_storage.Data.Id);

            if (nextEntry != null)
            {
                _storage.Data.SetId(nextEntry.Value.id);
                _storage.Data.SetProgress(0);
                _storage.Data.SetIsClaimed(false);
            }

            _storage.Save();
        }
    }
}