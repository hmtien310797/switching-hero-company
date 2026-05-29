using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Common
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        [field: SerializeField] public string BaseUrl { get; private set; } = "http://171.244.44.71:8082";

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}