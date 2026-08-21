namespace Immortal_Switch.Scripts.Modules.Cache.Global
{
    /// <summary>
    /// Base class cho cache cluster — wrap GlobalCache cho 1 type T cụ thể. Derived class chỉ cần
    /// kế thừa + thêm method typed (Has*/Mark*). Init() nạp cache 1 lần ở ModuleManager.
    /// </summary>
    public abstract class GlobalCacheModule<T> where T : class, new()
    {
        private bool _initialized;

        /// <summary>Nạp cache từ ES3 — idempotent (chỉ nạp 1 lần).</summary>
        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            GlobalCache.Init<T>();
        }

        /// <summary>Lấy data theo key; null nếu chưa có.</summary>
        public T Get(string key)
        {
            return GlobalCache.Get<T>(key);
        }

        /// <summary>Lấy data theo key, tạo mới (và thêm vào cache) nếu chưa có.</summary>
        public T GetOrCreate(string key)
        {
            return GlobalCache.GetOrCreate<T>(key);
        }

        /// <summary>Ghi toàn bộ cache của type T xuống ES3.</summary>
        public void Save()
        {
            GlobalCache.Save<T>();
        }
    }
}