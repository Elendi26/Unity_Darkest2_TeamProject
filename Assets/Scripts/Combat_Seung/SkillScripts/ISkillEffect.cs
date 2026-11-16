using JetBrains.Annotations;
using UnityEngine;

public interface ISkillEffect
{
    public abstract void SkillEffect(CEntityRuntimeData target, bool critical = false, bool hit = true);
}

public enum EDamageoverTime
{
    Bleed,
    Blight,
    Burn
}

[System.Serializable]
public class CSkillDamage : ISkillEffect
{
    #region 인스펙터

    [SerializeField] private int _minDamage;
    [SerializeField] private int _maxDamage;
    [SerializeField] private EInteractCombo _combo;

    #endregion

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {
        if (!hit)
        {
            // 빗나감

            return;
        }

        switch (_combo)
        {
            case EInteractCombo.None:

                if (critical)
                {
                    float CriticalDamage = _maxDamage * 1.5f;

                    target.ModifyStat(EEntityStat.HP, -CriticalDamage);

                    return;
                }

                float Damage = Random.Range(0, _maxDamage - _minDamage + 1);

                target.ModifyStat(EEntityStat.HP, -Damage);

                break;
            case EInteractCombo.True:

                int checkCombo = 0;

                checkCombo += (int)ETokenBitMask.Combo;

                if (((int)(target._tokenStatus.TokenBitMask) & checkCombo) != 0)
                {

                }

                break;
            case EInteractCombo.False:


                break;
            default:
                break;
        }


    }
}

[System.Serializable]
public class CSkillHeal : ISkillEffect
{
    #region 인스펙터

    [SerializeField, Range(0f, 100f)] private float _healPercent;
    [SerializeField, Range(0f, 100f)] private float _hpBelow;
    [SerializeField] private EInteractCombo _combo;

    #endregion

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {
        if (!hit)
        {
            return;
        }

        if ((target.GetStat(EEntityStat.HP) / target.GetStat(EEntityStat.MaxHP)) >= (_hpBelow / 100))
        {
            return;
        }

        float heal = target.GetStat(EEntityStat.MaxHP) * (_healPercent / 100);

        if (critical)
        {
            float criticalHeal = heal * 1.5f;

            target.ModifyStat(EEntityStat.HP, criticalHeal);

            return;
        }

        target.ModifyStat(EEntityStat.HP, heal);
    }


}

[System.Serializable]
public class CAddToken : ISkillEffect // 토큰 적용
{
    [SerializeField] private EInteractCombo _combo;

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {

    }
}

[System.Serializable]
public class CRemoveToken : ISkillEffect
{
    [SerializeField] private EInteractCombo _combo;

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {

    }
}


[System.Serializable]
public class CAddTokenDamageoveTime : ISkillEffect
{
    [Header("지속 피해 정보")]
    [SerializeField] private EDamageoverTime _tokenName;
    [SerializeField] private ETokenTriggerPoint _tokenTriggerPoint;
    [SerializeField] private EInteractCombo _combo;
    [SerializeField] private int _tokenDamage;
    [SerializeField] private int _tokenStack;

    public SDamageoverTime MakeTokenInstance()
    {
        SDamageoverTime damageoverTime = new SDamageoverTime(_tokenName, _tokenTriggerPoint, _tokenDamage, _tokenStack);

        return damageoverTime;
    }

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {

    }
}

[System.Serializable]
public class CChangeHitRate : ISkillEffect
{
    [SerializeField] private EInteractCombo _combo;

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {
        // 아직 미정
    }
}

[System.Serializable]
public class CAddComboToken : ISkillEffect
{
    [SerializeField] private EInteractCombo _combo;

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {

    }
}
