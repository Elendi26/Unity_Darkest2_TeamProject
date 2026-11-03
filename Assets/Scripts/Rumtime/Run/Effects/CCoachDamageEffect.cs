using System;
using UnityEngine;

[Serializable]
public class CCoachDamageEffect : IRunEffect
{
    #region 인스펙터
    [SerializeField] private int _amount = 1;
    #endregion

    public void Apply(CRunContext ctx)
    {
        if (ctx == null || ctx.RunData == null || _amount <= 0)
        {
            return;
        }

        int absorbed = -ctx.RunData.Add(ERunValue.Armor, -_amount);
        int remain = _amount - absorbed;

        if (remain <= 0)
        {
            return;
        }

        ctx.RunData.Add(ERunValue.Wheels, -remain);
    }
}
