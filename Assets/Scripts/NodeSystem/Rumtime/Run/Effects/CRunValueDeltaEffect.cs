using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CRunValueDeltaEffect : IRunEffect
{
    #region ¿ŒΩ∫∆Â≈Õ
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
