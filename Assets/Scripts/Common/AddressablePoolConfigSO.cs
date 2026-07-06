using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pooling
{
    [Serializable]
    public class AddressablePoolEntry
    {
        public string key;
        public int warmupCount = 5;
        public Transform parent;
    }

    [CreateAssetMenu(
        fileName = "AddressablePoolConfig",
        menuName = "Immortal Switch/Pooling/Addressable Pool Config")]
    public class AddressablePoolConfigSO : ScriptableObject
    {
        public AddressablePoolEntry[] entries;
    }
}