using System.Collections.Generic;
using UnityEngine;


public class CObjectPoolManager : CManagerSingleton<CObjectPoolManager>
{
    protected override bool IsDontDestroy => true;

    private Dictionary<GameObject, CObjectPool<GameObject>> _prefabLookup;
    private Dictionary<GameObject, CObjectPool<GameObject>> _instanceLookup;
    private Dictionary<GameObject, Transform> _parentLookup;

    protected override void Awake()
    {
        base.Awake();
        _prefabLookup = new Dictionary<GameObject, CObjectPool<GameObject>>();
        _instanceLookup = new Dictionary<GameObject, CObjectPool<GameObject>>();
        _parentLookup = new Dictionary<GameObject, Transform>();
    }

    /// <summary>
    /// 특정 프리팹에 대한 오브젝트 풀을 미리 생성(Warm)합니다.
    /// </summary>
    /// <param name="prefab">대상 프리팹 원본</param>
    /// <param name="size">생성할 개수</param>
    public void WarmPool(GameObject prefab, int size)
    {
        CObjectPool<GameObject> pool;
        if (_prefabLookup.ContainsKey(prefab))
        {
            pool = _prefabLookup[prefab];
            if (pool.Count < size)
            {
                pool.Warm(size - pool.Count);
            }
            CPrint.Log($"프리팹 [{prefab.name}]에 대한 오브젝트 풀이 이미 존재합니다.");
        }
        else
        {
            pool = new CObjectPool<GameObject>(() => { return InstantiatePrefab(prefab); }, DestroyObject, size, prefab);
        }
        _prefabLookup[prefab] = pool;
    }

    /// <summary>
    /// 특정 프리팹의 오브젝트 풀과 관리용 부모 계층을 파괴합니다.
    /// </summary>
    /// <param name="prefab">대상 프리팹 원본</param>
    public void DestroyPool(GameObject prefab)
    {
        if (_prefabLookup.ContainsKey(prefab))
        {
            var pool = _prefabLookup[prefab];
            pool.DestroyPool();
            _prefabLookup.Remove(prefab);
        }

        if (_parentLookup.ContainsKey(prefab))
        {
            Destroy(_parentLookup[prefab].gameObject);
            _parentLookup.Remove(prefab);
        }
    }

    /// <summary>
    /// 기본 위치와 회전값으로 프리팹을 스폰합니다.
    /// </summary>
    /// <param name="prefab">대상 프리팹 원본</param>
    public GameObject SpawnObject(GameObject prefab)
    {
        return SpawnObject(prefab, Vector3.zero, Quaternion.identity);
    }

    /// <summary>
    /// 지정한 위치와 회전값으로 프리팹을 스폰합니다.
    /// </summary>
    /// <param name="prefab">대상 프리팹 원본</param>
    /// <param name="position">생성 위치</param>
    /// <param name="rotation">회전값</param>
    public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            CPrint.Warn("스폰하려는 프리팹이 Null입니다.");
            return null;
        }

        if (!_prefabLookup.ContainsKey(prefab))
        {
            WarmPool(prefab, 1);
        }

        var pool = _prefabLookup[prefab];
        var clone = pool.GetItem();

        if (clone != null)
        {
            clone.transform.SetPositionAndRotation(position, rotation);
            clone.SetActive(true);

            if (!_instanceLookup.ContainsKey(clone))
            {
                _instanceLookup.Add(clone, pool);
            }
        }

        return clone;
    }

    /// <summary>
    /// 사용이 끝난 게임 오브젝트를 오브젝트 풀로 안전하게 반환합니다.
    /// </summary>
    /// <param name="clone">반환할 인스턴스</param>
    public void ReleaseObject(GameObject clone)
    {
        if (clone == null)
        {
            return;
        }

        clone.SetActive(false);

        if (_instanceLookup.ContainsKey(clone))
        {
            var pool = _instanceLookup[clone];

            // 반환 시 오브젝트 풀 부모로 재설정
            if (_parentLookup.ContainsKey(pool.Original))
            {
                clone.transform.parent = _parentLookup[pool.Original];
            }

            // 스케일 원본으로 복원
            if (pool.Original != null)
            {
                clone.transform.localScale = pool.Original.transform.localScale;
            }

            pool.ReleaseItem(clone);
            _instanceLookup.Remove(clone);
        }
        else
        {
            CPrint.Warn("풀 관리 목록에 존재하지 않는 오브젝트 반환 시도: " + clone.name);
        }
    }

    private GameObject InstantiatePrefab(GameObject prefab)
    {
        var go = Instantiate(prefab) as GameObject;

        Transform parent = transform;
        if (_parentLookup.ContainsKey(prefab))
        {
            parent = _parentLookup[prefab];
        }
        else
        {
            var parentGo = new GameObject("_Pool_" + prefab.name);
            parent = parentGo.transform;
            parent.parent = transform;
            _parentLookup.Add(prefab, parent);
        }

        go.transform.parent = parent;
        go.SetActive(false);
        return go;
    }

    private void DestroyObject(GameObject clone)
    {
        if (_instanceLookup.ContainsKey(clone))
        {
            _instanceLookup.Remove(clone);
        }
        Destroy(clone);
    }
}