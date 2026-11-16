using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ICombatRuntime
{
    public void None (ref CEntityRuntimeData entityRuntimeData);

    public void Critical (ref CEntityRuntimeData entityRuntimeData);

    public void Guard (ref CEntityRuntimeData entityRuntimeData);

    public void UseToTarget (ref CEntityRuntimeData entityRuntimeData);

    public void Targeted (ref CEntityRuntimeData entityRuntimeData);

    public void DamageToTarget(ref CEntityRuntimeData entityRuntimeData);

    public void Damaged (ref CEntityRuntimeData entityRuntimeData);

    public void RoundStart (ref CEntityRuntimeData entityRuntimeData);

    public void TurnStart (ref CEntityRuntimeData entityRuntimeData);

    public void TurnOver(ref CEntityRuntimeData entityRuntimeData);
    
    public void Combo (ref CEntityRuntimeData entityRuntimeData);
}


public class CSkillExcuter // : ICombatRuntime
{
    // 스킬 사용 후, 토큰 기반 명중/치명타 및 각종 확률 적용

    public CSkillExcuter()
    {

    }
}
