using UnityEngine;
public class PersistentSingleton<T> : MonoBehaviour where T : Component
{
	protected static T _instance;
    static bool _isDestroyed;
	public static T instance
	{
		get
		{
            if (_isDestroyed)
                return null;
			if (_instance == null)
			{
				_instance = FindObjectOfType<T> ();
				if (_instance == null)
				{
					GameObject obj = new GameObject ();
					_instance = obj.AddComponent<T> ();
                    obj.transform.name = typeof(T).ToString();
                }
			}
			return _instance;
		}
	}

	protected virtual void Awake ()
	{
        if (_instance == null
			|| _instance == this)
		{
			//If I am the first instance, make me the Singleton
			_instance = this as T;
			// DontDestroyOnLoad (transform.gameObject);
		}
		else
		{
			//If a Singleton already exists and you find
			//another reference in scene, destroy it!
			if(this != _instance)
			{
				Destroy(this.gameObject);
			}
		}
	}
    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _isDestroyed = true;
    }
}
