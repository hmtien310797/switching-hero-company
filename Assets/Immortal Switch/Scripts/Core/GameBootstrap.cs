using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Skill.UI;
using Scripts.Battle;
using Scripts.Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private async void Start()
        {
            Application.targetFrameRate = 60;
            await RunAsync();
        }

        private async UniTask RunAsync()
        {
            await MasterDataCache.Instance.InitializeAsync();
            await UserDataCache.Instance.InitializeAsync();
            await GrowthManager.Instance.InitializeAsync();
            await PowerUpManager.Instance.InitializeAsync();
            await UIManager.Instance.InitializeAsync();
            await SkillViewDataProvider.Instance.InitializeAsync();
            await PvEBattleController.Instance.InitializeAsync();

            Debug.Log("Bootstrap completed");
        }
    }
}
