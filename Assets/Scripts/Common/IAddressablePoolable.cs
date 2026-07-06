namespace Immortal_Switch.Scripts.Pooling
{
    public interface IAddressablePoolable
    {
        void OnSpawned(AddressablePoolHandle handle);
        void OnDespawned();
    }
}