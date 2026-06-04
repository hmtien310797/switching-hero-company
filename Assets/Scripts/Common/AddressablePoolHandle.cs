using System;
using Addler.Runtime.Core.Pooling;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pooling
{
    public sealed class AddressablePoolHandle : IDisposable
    {
        private readonly AddressablePoolService service;
        private readonly string key;
        private readonly PooledObject pooledObject;

        private bool disposed;

        public GameObject Instance => pooledObject.Instance;

        internal AddressablePoolHandle(
            AddressablePoolService service,
            string key,
            PooledObject pooledObject)
        {
            this.service = service;
            this.key = key;
            this.pooledObject = pooledObject;
        }

        public void Despawn()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            service.ReturnInternal(key, pooledObject);
        }
    }
}