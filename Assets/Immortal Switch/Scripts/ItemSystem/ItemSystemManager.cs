using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;

namespace Immortal_Switch.Scripts.ItemSystem
{
    public class ItemSystemManager : Singleton<ItemSystemManager>
    {
        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}