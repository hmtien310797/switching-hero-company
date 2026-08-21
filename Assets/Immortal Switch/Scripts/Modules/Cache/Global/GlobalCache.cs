using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Modules.Cache.Global
{
    /// <summary>
    /// Global cache registry — mọi cache data truy cập qua đây bằng Get&lt;T&gt;(key). Mỗi type T
    /// lưu riêng 1 ES3 key (tự sinh từ tên type). Init&lt;T&gt;() nạp 1 lần ở ModuleManager.
    /// Không cần giữ instance cache riêng lẻ ở đâu cả.
    /// </summary>
    public static class GlobalCache
    {
        public static void Init<T>() where T : class, new()
        {
            Store<T>.Instance.Load();
        }

        /// <summary>Lấy data theo key; null nếu chưa có.</summary>
        public static T Get<T>(string key) where T : class, new()
        {
            return Store<T>.Instance.Get(key);
        }

        /// <summary>Lấy data theo key, tạo mới (và thêm vào cache) nếu chưa có.</summary>
        public static T GetOrCreate<T>(string key) where T : class, new()
        {
            return Store<T>.Instance.GetOrCreate(key);
        }

        /// <summary>Ghi toàn bộ cache của type T xuống ES3.</summary>
        public static void Save<T>() where T : class, new()
        {
            Store<T>.Instance.Save();
        }

        private static class Store<T> where T : class, new()
        {
            public static readonly string SaveKey = $"global_cache_{typeof(T).Name}";
            public static readonly GlobalCacheStore<T> Instance = new(SaveKey);
        }
    }

    internal class GlobalCacheStore<T> where T : class, new()
    {
        private readonly string _saveKey;
        private Dictionary<string, T> _data;

        public GlobalCacheStore(string saveKey)
        {
            _saveKey = saveKey;
        }

        public void Load()
        {
            _data = ES3.Load(_saveKey, new Dictionary<string, T>());
        }

        public T Get(string key)
        {
            EnsureLoaded();
            return key != null && _data.TryGetValue(key, out var value) ? value : null;
        }

        public T GetOrCreate(string key)
        {
            EnsureLoaded();

            if (key == null)
            {
                return null;
            }

            if (!_data.TryGetValue(key, out var value))
            {
                value = new T();
                _data[key] = value;
            }

            return value;
        }

        public void Save()
        {
            EnsureLoaded();
            ES3.Save(_saveKey, _data);
        }

        private void EnsureLoaded()
        {
            if (_data == null)
            {
                Load();
            }
        }
    }
}