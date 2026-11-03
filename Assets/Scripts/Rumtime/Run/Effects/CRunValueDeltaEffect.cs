using System;
using UnityEngine;

[Serializable]
public class CRunValueDeltaEffect : IRunEffect
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

    public void Apply(CRunContext ctx)
    {
        if (ctx == null || ctx.RunData == null)
        {
            return;
        }

        ctx.RunData.Add(_type, _delta);
    }
}
