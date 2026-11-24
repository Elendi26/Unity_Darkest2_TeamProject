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

    [Header("CCombatCurrentTurn 스크립트 참조")]
    [SerializeField] private CCombatCurrentTurn _currentTurn;

    // 여기에 활성화/비활성화 시키고 싶은 것을 넣고
    [Header("현재 턴 표시 UI")]
    [SerializeField] private GameObject _currentTurnIndicator;

    [Header("타겟 표시기")]
    [SerializeField] private GameObject _targetIndicator;



    private int _rank;
    private bool _targetable = false;
    private ICombatData _combatData;
    
    #region 프로퍼티
    public ICombatData CombatData => _combatData;
    public bool Targetable => _targetable;

    #endregion

    private void Awake()
    {
        if(_currentTurnIndicator != null)
        {
            _currentTurnIndicator.SetActive(false);
        }

        if(_targetIndicator != null)
        {
            _targetIndicator.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if(_currentTurn != null)
        {
            _currentTurn.ChangedEntityTurn += OnOffIndicator;
        }

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
        if (_currentTurn != null)
        {
            _currentTurn.ChangedEntityTurn -= OnOffIndicator;
        }


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

        if(_targetIndicator != null)
        {
            _targetIndicator.SetActive(_targetable); 
        }
        

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

    private void OnOffIndicator(CEntityRuntimeData data)
    {
        if (_entityRuntimeData == data)
        {
            _currentTurnIndicator.SetActive(true);
        }

        else
        {
            _currentTurnIndicator.SetActive(false);
        }
    }

    public void ResetTargetRank()
    {
        _targetable = false;
        if(_targetIndicator != null)
        {
            _targetIndicator.SetActive(false);
        }
    }

}
