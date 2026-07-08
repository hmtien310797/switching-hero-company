using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Modules.Power.Services;
using Immortal_Switch.Scripts.Modules.Power.Services.Interfaces;

namespace Immortal_Switch.Scripts.Modules
{
    public class ModuleManager : Singleton<ModuleManager>
    {
        public IPowerService PowerService { get; } = new PowerService();

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}