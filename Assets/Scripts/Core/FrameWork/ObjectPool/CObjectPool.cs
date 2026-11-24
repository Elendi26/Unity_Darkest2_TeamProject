using System;
using System.Collections.Generic;
using UnityEngine;

public class CObjectPool<T> where T : class
{
    private List<CObjectPoolContainer<T>> _list;
    private Dictionary<T, CObjectPoolContainer<T>> _lookup;
    private Func<T> _factoryFunc;
    private Action<T> _destroyFunc;

    /// <summary>
    /// 풀 생성 시 기준이 되는 원본 객체
    /// </summary>
    public T Original { get; private set; }
    private int _lastIndex = 0;

    /// <summary>
    /// CObjectPool 생성자
    /// </summary>
    /// <param name="factoryFunc">객체 생성 함수</param>
    /// <param name="destroyFunc">객체 파괴 함수</param>
    /// <param name="initialSize">초기 풀 크기</param>
    /// <param name="origin">원본 객체</param>
    public CObjectPool(Func<T> factoryFunc, Action<T> destroyFunc, int initialSize, T origin = null)
    {
        _factoryFunc = factoryFunc;
        _destroyFunc = destroyFunc;
        Original = origin;
        _list = new List<CObjectPoolContainer<T>>(initialSize);
        _lookup = new Dictionary<T, CObjectPoolContainer<T>>(initialSize);
        Warm(initialSize);
    }

    /// <summary>
    /// 지정한 용량만큼 풀을 미리 채웁니다 (Pre-warm).
    /// </summary>
    /// <param name="capacity">확장할 개수</param>
    public void Warm(int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            CreateContainer();
        }
    }

    /// <summary>
    /// 풀에 있는 모든 객체를 파괴하고 메모리를 정리합니다.
    /// </summary>
    public void DestroyPool()
    {
        while (_list.Count != 0)
        {
            var item = _list[0].Item;

            if (_lookup.ContainsKey(item))
            {
                _lookup.Remove(item);
            }

            _destroyFunc?.Invoke(item);
            _list.RemoveAt(0);
        }
    }

    private CObjectPoolContainer<T> CreateContainer()
    {
        var container = new CObjectPoolContainer<T>();
        container.Item = _factoryFunc();
        _list.Add(container);
        return container;
    }

    /// <summary>
    /// 풀에서 사용 가능한 아이템을 가져옵니다.
    /// </summary>
    public T GetItem()
    {
        CObjectPoolContainer<T> container = null;
        for (int i = 0; i < _list.Count; i++)
        {
            _lastIndex++;
            if (_lastIndex > _list.Count - 1)
            {
                _lastIndex = 0;
            }

            if (_list[_lastIndex].Used)
            {
                continue;
            }
            else
            {
                container = _list[_lastIndex];
                break;
            }
        }

        if (container == null)
        {
            container = CreateContainer();
        }

        container.Consume();
        _lookup.Add(container.Item, container);
        return container.Item;
    }

    /// <summary>
    /// 사용이 끝난 아이템을 풀로 반환합니다.
    /// </summary>
    /// <param name="item">반환할 아이템</param>
    public void ReleaseItem(T item)
    {
        if (_lookup.ContainsKey(item))
        {
            var container = _lookup[item];
            container.Release();
            _lookup.Remove(item);
        }
        else
        {
            CPrint.Warn("해당 오브젝트 풀에 존재하지 않는 아이템 반환 시도: " + item);
        }
    }

    /// <summary>
    /// 전체 풀에 등록된 아이템 총 개수
    /// </summary>
    public int Count => _list.Count;

    /// <summary>
    /// 현재 사용 중인 아이템 개수
    /// </summary>
    public int CountUsedItems => _lookup.Count;
}