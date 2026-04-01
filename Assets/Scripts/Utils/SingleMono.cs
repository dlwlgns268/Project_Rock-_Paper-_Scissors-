using UnityEngine;

namespace Utils
{
    public class SingleMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance
        {
            get
            {
                if (!_instance) _instance = FindAnyObjectByType<T>();
                if (!_instance) _instance = new GameObject().AddComponent<T>();
                return _instance;
            }
        }
        private static T _instance;
        
        private void Awake()
        {
            if (_instance)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}
