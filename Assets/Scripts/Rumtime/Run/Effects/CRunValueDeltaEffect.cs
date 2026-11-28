using System;
using UnityEngine;

[Serializable]
public class CRunValueDeltaEffect : IRunEffect, IRunEffectText
{
    #region 인스펙터
    [SerializeField] private ERunValue _type = ERunValue.Wheels;
    [SerializeField] private int _delta = -1;
    #endregion

    public CRunValueDeltaEffect()
    {
        // Do Nothing
    }

    public CRunValueDeltaEffect(ERunValue type, int delta)
    {
        _type = type;
        _delta = delta;
    }

    // 예: 바퀴 +1, 횃불 -10
    public string GetText()
    {
        if (_delta == 0)
        {
            return null;
        }

        return $"{CRunValueTable.GetName(_type)} {(_delta > 0 ? "+" : string.Empty)}{_delta}";
    }

    public void Apply(CRunContext ctx)
    {
        if (ctx == null || ctx.RunData == null)
        {
            return;
        }

        ctx.RunData.Add(_type, _delta);
    }
}
