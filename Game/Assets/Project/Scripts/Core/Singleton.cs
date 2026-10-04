using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    #region Fields
    private static T _instance;
    private static bool _isQuitting;
    #endregion

    #region Properties
    public static bool HasInstance => _instance != null;

    public static T Instance
    {
        get
        {
            if(_instance != null) 
                return _instance;

            if(_isQuitting)
                return null;

            _instance = FindFirstObjectByType<T>();
            if(_instance != null)
                return _instance;
            
            GameObject obj = new GameObject(typeof(T).Name);
            _instance = obj.AddComponent<T>();
            DontDestroyOnLoad(obj);
            return _instance;
        }
    }
    #endregion

    #region Methods
    protected virtual void Awake()
    {
        if(_instance == null)
        {
            _instance = this as T;
            _isQuitting = false;
            Application.quitting += OnApplicationQuitting;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if(_instance == this)
        {
            Application.quitting -= OnApplicationQuitting;
            _instance = null;
        }
    }

    private static void OnApplicationQuitting()
    {
        _isQuitting = true;
    }
    #endregion
}