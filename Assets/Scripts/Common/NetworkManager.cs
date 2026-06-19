using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Common
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        [field: SerializeField] public string BaseUrl { get; private set; }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}