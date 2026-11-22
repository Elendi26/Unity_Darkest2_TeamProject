using UnityEngine;

public enum ECombatRank
{
    One,
    Two,
    Three,
    Four
}

/// <summary>
/// 각 칸에 CCombatRank를 붙이기
/// </summary>
public class CCombatRank : MonoBehaviour
{
    [SerializeField] private ECombatRank _combatRank;
    [SerializeField] private bool _team;
    [SerializeField] private CEntityRuntimeData _entityRuntimeData; // 테스트용으로 열어둠, 원래는 SerializeField 없어도 됨

    // 여기에 활성화/비활성화 시키고 싶은 것을 넣고

    private int _rank;
    private bool _targetable = false;
    private ICombatData _combatData;
    
    #region 프로퍼티
    public ICombatData CombatData => _combatData;
    public bool Targetable => _targetable;

    #endregion


    private void OnEnable()
    {
        ERank rank = ERank.None;

        switch (_combatRank)
        {
            case ECombatRank.One:

                
                rank |= ERank.One;

                _rank = (int)rank;

                break;
            case ECombatRank.Two:

                rank |= ERank.Two;

                _rank = (int)rank;

                break;
            case ECombatRank.Three:

                rank |= ERank.Three;
                _rank = (int)rank;

                break;

            case ECombatRank.Four:

                rank |= ERank.Four;
                _rank = (int)rank;

                break;
            default:
                break;
        }
    }

    private void OnDisable()
    {
        _rank = (int)ERank.None;
    }

    public void InputEntityData(CEntityRuntimeData entity)
    {
        _entityRuntimeData = entity;
    }

    public void ChangeEntityToCorpse()
    {
        if (!_entityRuntimeData.IsHero())
        {
            // 시체로 바꾸기
        }
    }

    public void CheckTargetRank(int skillTarget, ESkillType type)
    { 

        switch (type)
        {
            case ESkillType.Melee:

                if (!_team)
                {
                    if ((skillTarget & _rank) != 0)
                    {
                        _targetable = true;
                    }

                    else
                    {
                        _targetable = false;
                    }
                }

                else
                {
                    _targetable = false;
                }

                    break;

            case ESkillType.Ranged:

                if (!_team)
                {
                    if ((skillTarget & _rank) != 0)
                    {
                        _targetable = true;
                    }

                    else
                    {
                        _targetable = false;
                    }
                }

                else
                {
                    _targetable = false;
                }

                break;

            case ESkillType.Buff:

                if (_team)
                {
                    if ((skillTarget & _rank) != 0)
                    {
                        _targetable = true;
                    }

                    else
                    {
                        _targetable = false;
                    }
                }

                else
                {
                    _targetable = false;
                }

                    break;

            case ESkillType.Heal:

                if (_team)
                {
                    if ((skillTarget & _rank) != 0)
                    {
                        _targetable = true;
                    }

                    else
                    {
                        _targetable = false;
                    }
                }

                else
                {
                    _targetable = false;
                }

                    break;

            default:
                break;
        }

        // UI Targetable 켜기

    }

    public CEntityRuntimeData GetEntityRuntimeData()
    {
        return _entityRuntimeData;
    }

    public int GetDataFromRank_int(EEntityStat status)
    {
        return Mathf.RoundToInt(_entityRuntimeData.GetStat(status));
    }

    public float GetDataFromRank_float(EEntityStat status)
    {
        return _entityRuntimeData.GetStat(status);
    }


}
