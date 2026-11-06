using UnityEngine;

public interface ISkillEffect
{
    public abstract void SkillEffect(CEntityRuntimeData target, bool critical = false, bool hit = true);
}

[System.Serializable]
public class CSkillDamage : ISkillEffect
{
    #region ÀÎ½ºÆåÅÍ

    [SerializeField] private int _minDamage;
    [SerializeField] private int _maxDamage;

    #endregion

    public void SkillEffect(CEntityRuntimeData target, bool critical, bool hit)
    {
        if (!hit)
        {
            // ºø³ª°¨

            return;
        }

        if (critical)
        {
            float CriticalDamage = _maxDamage * 1.5f;

            target.ModifyStat(EEntityStat.HP, -CriticalDamage);

            return;
        }

        float Damage = Random.Range(0, _maxDamage - _minDamage + 1);

        target.ModifyStat(EEntityStat.HP, -Damage);
    }
}

[System.Serializable]
public class CSkillHeal : ISkillEffect
{
    #region ÀÎ½ºÆåÅÍ

    [SerializeField, Range(0f, 100f)] private float _healPercent;
    [SerializeField, Range(0f, 100f)] private float _hpBelow;

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

