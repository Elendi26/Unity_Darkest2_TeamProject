using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CDataDatabaseSO<T> : ScriptableObject, ISerializationCallbackReceiver where T : CGameDataSO
{
    #region 인스펙터
    [SerializeField] private List<T> _datas = new List<T>();
    #endregion

    #region 내부 변수
    [NonSerialized] private Dictionary<string, T> _lookup;
    [NonSerialized] private bool _isDirty = true;
    #endregion

    #region 프로퍼티
    public IReadOnlyList<T> Datas => _datas;
    public int Count => _datas.Count;
    #endregion

    #region 인덱서
    public T this[int index] => _datas[index];
    #endregion

    /// <summary>
    /// ID를 통해 데이터를 검색합니다.
    /// </summary>
    /// <param name="id">검색할 데이터 ID</param>
    /// <param name="data">찾은 데이터 결과</param>
    public bool TryGet(string id, out T data)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            data = null;
            return false;
        }

        EnsureLookup();
        return _lookup.TryGetValue(id, out data);
    }

    private void EnsureLookup()
    {
        if (!_isDirty && _lookup != null)
        {
            return;
        }

        _lookup ??= new Dictionary<string, T>();
        _lookup.Clear();

        for (int i = 0; i < _datas.Count; i++)
        {
            T data = _datas[i];

            if (data == null || string.IsNullOrWhiteSpace(data.Id))
            {
                continue;
            }

            _lookup.TryAdd(data.Id, data);
        }

        _isDirty = false;
    }

    protected virtual void OnValidate()
    {
        HashSet<string> ids = new HashSet<string>();

        for (int i = 0; i < _datas.Count; i++)
        {
            T data = _datas[i];

            if (data == null)
            {
                continue;
            }

            string id = data.Id;

            if (string.IsNullOrWhiteSpace(id))
            {
                CPrint.Warn($"{name} / {i} → Data의 ID가 비어 있습니다.");
                continue;
            }

            if (!ids.Add(id))
            {
                CPrint.Warn($"{name} : 중복 ID 발견 {id}");
            }
        }

        _isDirty = true;
    }

    public void OnBeforeSerialize()
    {
        // Do Nothing
    }

    public void OnAfterDeserialize()
    {
        _isDirty = true;
    }
}