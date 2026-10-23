using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRunValue
{
    #region 이벤트
    public event Action<ERunValue, int, int> OnChanged;
    #endregion

    #region 내부 변수
    private readonly ERunValue _type;
    private int _current;
    private int _max;
    #endregion

    public ERunValue Type => _type;
    public int Current => _current;
    public int Max
    {
        get => _max;
        set
        {
            _max = Mathf.Max(0, value);
            SetCurrent(_current);
        }
    }

    public float Ratio => _max > 0 ? (float)_current / _max : 0f;
    public bool IsEmpty => _current <= 0;

    public CRunValue(ERunValue type, int current, int max)
    {
        _type = type;
        _max = Mathf.Max(0, max);
        _current = Mathf.Clamp(current, 0, _max);
    }

    public int Add(int delta)
    {
        int prev = _current;

        SetCurrent(_current + delta);
        return _current - prev;
    }

    private void SetCurrent(int value)
    {
        int prev = _current;

        _current = Mathf.Clamp(value, 0, _max);

        if (prev == _current)
        {
            return;
        }

        OnChanged?.Invoke(_type, prev, _current);
    }
}
