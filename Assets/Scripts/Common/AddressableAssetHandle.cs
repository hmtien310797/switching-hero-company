using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Common
{
    public sealed class AddressableAssetHandle<T> : IDisposable
        where T : UnityEngine.Object
    {
        private AsyncOperationHandle<T> operationHandle;
        private bool isReleased;

        public T Asset { get; }

        public bool IsValid =>
            !isReleased &&
            operationHandle.IsValid();

        internal AddressableAssetHandle(
            AsyncOperationHandle<T> operationHandle,
            T asset)
        {
            this.operationHandle = operationHandle;
            Asset = asset;
        }

        public void Release()
        {
            if (isReleased)
                return;

            isReleased = true;

            if (operationHandle.IsValid())
            {
                Addressables.Release(operationHandle);
            }
        }

        public void Dispose()
        {
            Release();
        }
    }
}