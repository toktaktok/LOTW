using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    #region Fields
    private static T _instance;
    #endregion

    #region Properties
    public static T Instance
    {
        get
        {
            if(_instance != null) 
                return _instance;

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
            _instance = null;
    }
    #endregion
}