using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRunData
{
    #region 이벤트
    public event Action<ERunValue, int, int> OnValueChanged;
    #endregion

    #region 내부 변수
    private readonly Dictionary<ERunValue, CRunValue> _values = new Dictionary<ERunValue, CRunValue>();
    #endregion

    public CRunValue Register(ERunValue type, int current, int max)
    {
        if (_values.TryGetValue(type, out CRunValue existing))
        {
            CPrint.Warn("중복 등록");
            return existing;
        }

        CRunValue value = new CRunValue(type, current, max);

        value.OnChanged += NotifyValueChanged;
        _values.Add(type, value);
        return value;
    }

    public CRunValue Get(ERunValue type)
    {
        return _values.TryGetValue(type, out CRunValue value) ? value : null;
    }

    public int GetCurrent(ERunValue type)
    {
        CRunValue value = Get(type);

        return value != null ? value.Current : 0;
    }

    public int Add(ERunValue type, int delta)
    {
        CRunValue value = Get(type);

        if (value == null)
        {
            return 0;
        }

        return value.Add(delta);
    }

    public bool Has(ERunValue type, int amount)
    {
        return GetCurrent(type) >= amount;
    }

    private void NotifyValueChanged(ERunValue type, int prev, int current)
    {
        OnValueChanged?.Invoke(type, prev, current);
    }
}
