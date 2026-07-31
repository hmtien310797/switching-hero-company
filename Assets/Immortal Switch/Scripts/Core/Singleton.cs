using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        // Ngăn Instance tự spawn 1 GameObject "ma" khi app/scene đang đóng (Stop Play Mode gọi
        // OnApplicationQuit TRƯỚC khi destroy các object trong scene) — nếu không, bất kỳ OnDestroy
        // nào của object khác lỡ đọc T.Instance sau khi instance thật đã bị huỷ sẽ tạo mới 1
        // GameObject không bao giờ được dọn, gây warning "Some objects were not cleaned up when
        // closing the scene".
        private static bool _applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindFirstObjectByType<T>();
                if (_instance != null)
                {
                    return _instance;
                }

                var go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }

        [SerializeField]
        protected bool DontDestroyOnLoadEnabled;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                if (DontDestroyOnLoadEnabled)
                    DontDestroyOnLoad(gameObject);

                OnSingletonAwake();
                return;
            }

            if (_instance != this)
                Destroy(gameObject);

            if (DontDestroyOnLoadEnabled)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnSingletonAwake() { }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public abstract UniTask InitializeAsync();
    }
}