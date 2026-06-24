using Immortal_Switch.Scripts.Pooling;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkillRuntimeObject))]
    public sealed class AddressableSkillRuntimePoolable
        : AddressablePoolableBehaviour
    {
        private SkillRuntimeObject runtimeObject;

        private void Awake()
        {
            runtimeObject =
                GetComponent<SkillRuntimeObject>();
        }

        public override void OnSpawned(
            AddressablePoolHandle handle)
        {
            base.OnSpawned(handle);

            if (runtimeObject == null)
            {
                runtimeObject =
                    GetComponent<SkillRuntimeObject>();
            }

            runtimeObject.BindAddressablePoolSpawn(handle);
        }

        public override void OnDespawned()
        {
            if (runtimeObject == null)
            {
                runtimeObject =
                    GetComponent<SkillRuntimeObject>();
            }

            runtimeObject.NotifyAddressablePoolDespawned();

            base.OnDespawned();
        }
    }
}