using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CManagerSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    var gO = new GameObject(typeof(T).ToString());
                    _instance = gO.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    public static T Create()
    {
        return CManagerSingleton<T>.Instance;
    }

    protected virtual bool IsDontDestroy => true;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            if (IsDontDestroy)
            {

                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

/*
//ㅇㅇ
다른 매니저에서 사용법
public class CGameManager : CManagerSingleton<CGameManager>

씬 전환시 파괴되어야 하는 매니저
protected override bool IsDontDestroy => false;

다른 스크립트에서 해당 매니저 사용
1. 미리 생성
CGameManager.Create();
2. 함수 호출
CGameManager.Instance.함수();
3. 변수 활용
자료형 변수 = CGameManager.Instance.참조변수;



*/