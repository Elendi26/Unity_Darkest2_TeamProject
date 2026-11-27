using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;
using static UnityEngine.GraphicsBuffer;

public class CCombatSelectedSkill : MonoBehaviour
{
    [Header("CCombatCurrentTurn 스크립트 참조")]
    [SerializeField] private CCombatCurrentTurn _currentTurn;

    // 원래 전체 8개를 한번에 리스트로 놨었는데, 팀원 / 적으로 나눠서 재구성함.
    [Header("아군 랭크 4개")]
    [SerializeField] private CCombatRank[] _teamRanks;
    [Header("아군 랭크 사이 + 아이콘")]
    [SerializeField] private GameObject[] _teamPlusIcons;

    [Header("적군 랭크 4개")]
    [SerializeField] private CCombatRank[] _enemyRanks;
    [Header("적군 랭크 사이 + 아이콘")]
    [SerializeField] private GameObject[] _enemyPlusIcons;

    private CEntityRuntimeData _entity;
    private CSkillDataSO _skillData;
    private CEntityAnimation _animation;

    private void Awake()
    {
        OffIndicator();

        _currentTurn.StateReset();
    }

    public void InputSelectedSkill(CEntityRuntimeData entity, ICombatData combatData, CEntityAnimation animation)
    {
        Debug.Log("영웅 스킬 사용");

        if (combatData is CSkillDataSO skillData)
        {
            _entity = entity;
            _skillData = skillData;
            _animation = animation;

            CheckTarget();
        }

        else
        {
            Debug.Log("CombatSelectedSkill/InputSelectedSkill : 스킬 데이터가 비었습니다");

            return;
        }
    }

    public void InputSelectedSkill(CEntityRuntimeData entity, ICombatData combatData, CEntityAnimation animation, CEntityRuntimeData target)
    {
        Debug.Log("적 스킬 사용");

        if (combatData is CSkillDataSO skillData)
        {
            _entity = entity;
            _skillData = skillData;
            _animation = animation;

            // 리스트로 한번 감싸서 전달 -> 타겟 자체를 범위공격 대비하여 리스트로 관리하게 만듬
            List<CEntityRuntimeData> targets = new List<CEntityRuntimeData>();
            if(target != null)
            {
                targets.Add(target);
            }

            SkillExcute(targets);

            CCombatManager.Instance.TurnOver();
        }

        else
        {
            Debug.Log("CombatSelectedSkill/InputSelectedSkill : 스킬 데이터가 비었습니다");

            return;
        }
    }

    /// <summary>
    /// 이동을 눌렀을 때
    /// </summary>
    /// <param name="mover"></param>
    public void InputMoveSlot(CEntityRuntimeData mover)
    {
        // 인디케이터 다 끄고
        OffIndicator();

        _entity = mover;

        // 현재 랭크를 정수로 가져오기
        int currentRank = (int)mover.GetRank();

        int moveMask = ((((int)currentRank << 1) | ((int)currentRank >> 1)) & 15) & ~(int)currentRank;

        if (_currentTurn.SelectType == ECombatSelectType.Move)
        {
            for (int i = 0; i < _teamRanks.Length; i++)
            {
                bool canMoveRank = (moveMask & (1 << i)) != 0 && _teamRanks[i].GetEntityRuntimeData() != null;

                if (canMoveRank)
                {
                    _teamRanks[i].SetTargetRank();

                }

            }
        }

    }

    private void CheckTarget()
    {
        CPrint.Log("스킬의 대상을 선택하세요");

        int targetBitmask = _skillData.GetTargetBitmask();

        for(int i = 0; i < _teamRanks.Length;  i++)
        {
            if(_teamRanks[i] != null)
            {
                _teamRanks[i].CheckTargetRank(targetBitmask, _skillData.Type);
            }

        }

        for(int j = 0;  j < _enemyRanks.Length; j++)
        {
            if (_enemyRanks[j] != null)
            {
                _enemyRanks[j].CheckTargetRank(targetBitmask, _skillData.Type);
            }

        }

        for(int k = 0; k < 3; k++)
        {
            _teamPlusIcons[k].SetActive(_skillData.AOE && _teamRanks[k].Targetable && _teamRanks[k + 1].Targetable);
            _enemyPlusIcons[k].SetActive(_skillData.AOE && _enemyRanks[k].Targetable && _enemyRanks[k + 1].Targetable);

        }

    }

    public void InputTargetData(ICombatData combatData)
    {
        if (combatData is CEntityRuntimeData target )
        {
            if(_currentTurn.SelectType != ECombatSelectType.Move)
            {
                if (!target.IsCorpse)
                {
                    for (int i = 0; i < _teamRanks.Length; i++)
                    {
                        if (
                            _teamRanks[i] != null && _teamRanks[i].Targetable == true && _teamRanks[i].GetEntityRuntimeData() == target ||
                            _enemyRanks[i] != null && _enemyRanks[i].Targetable == true && _enemyRanks[i].GetEntityRuntimeData() == target
                            )
                        {
                            SkillExcute(target);

                            OffIndicator();

                            _currentTurn.StateReset();

                            CCombatManager.Instance.TurnOver();

                            return;
                        }

                    }
                }
                
            }

            // 이동버튼 눌렀을 경우
            else if(_currentTurn.SelectType == ECombatSelectType.Move)
            {
                CCombatRank fromRank = null;

                for (int i = 0; i < _teamRanks.Length; i++)
                {
                    if( _teamRanks[i] != null && _teamRanks[i].Targetable == true && _teamRanks[i].GetEntityRuntimeData() == target)
                    {

                        CCombatRank toRank = _teamRanks[i];

                        for(int j = 0; j < _teamRanks.Length; j++)
                        {
                            if(_teamRanks[j].GetEntityRuntimeData() == _entity)
                            {
                                fromRank = _teamRanks[j];

                                break;
                            }
                        }

                        if( fromRank == null)
                        {
                            return;
                        }

                        ERank tempRank = _entity.GetRank();
                        
                        _entity.ChangeRank(target.GetRank());

                        target.ChangeRank(tempRank);
                        fromRank.InputEntityData(target);
                        toRank.InputEntityData(_entity);

                        Transform moverTransform = fromRank.EntityTransform;
                        Transform targetTransform = toRank.EntityTransform;

                        if (moverTransform != null && targetTransform != null)
                        {
                            Vector3 tempPos = moverTransform.position;
                            moverTransform.position = targetTransform.position;
                            targetTransform.position = tempPos;
                        }

                        fromRank.InputEntityTransform(targetTransform);
                        toRank.InputEntityTransform(moverTransform);

                        OffIndicator();

                        _currentTurn.StateReset();

                        CCombatManager.Instance.TurnOver();

                        return;
                    }
                }
            }

        }
    }

    public void InputEveryTargetData()
    {
        List<CEntityRuntimeData> targets = new List<CEntityRuntimeData>();

        for (int i = 0; i < _teamRanks.Length; i++)
        {
            if (_teamRanks[i].Targetable && _teamRanks[i] != null)
            {
                if(!_entity.IsDead())
                {                
                    targets.Add(_teamRanks[i].GetEntityRuntimeData());
                }

            }
        }

        for (int j = 0; j < _enemyRanks.Length; j++)
        {
            if (_enemyRanks[j].Targetable && _enemyRanks[j] != null)
            {
                if (!_entity.IsDead())
                {
                    targets.Add(_enemyRanks[j].GetEntityRuntimeData());
                }

            }
        }

        SkillExcute(targets);

        OffIndicator();

        _currentTurn.StateReset();

        CCombatManager.Instance.TurnOver();
    }


    private bool CheckHit(CEntityRuntimeData target)
    {
        float hit = 100f;

        if (_entity._tokenStatus.CheckToken(ETokenBitMask.Blind))
        {
            hit *= 0.5f;
        }

        if (target._tokenStatus.CheckToken(ETokenBitMask.Dodge))
        {
            hit *= 0.5f;
        }

        if (target._tokenStatus.CheckToken(ETokenBitMask.DodgeE))
        {
            hit *= 0.25f;
        }

        float rand = Random.Range(0, 100f);

        if (rand <= hit)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    private bool CheckCritical(CEntityRuntimeData target)
    {
        if (_entity._tokenStatus.CheckToken(ETokenBitMask.Crit))
        {
            return true;
        }

        float ciritcal = (_skillData.Critical) + _entity.GetStat(EEntityStat.ExtraCritical);

        float rand = Random.Range(0, 100);

        if (rand <= ciritcal)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    private void SkillExcute(CEntityRuntimeData target)
    {
        Coroutine coroutine = StartCoroutine(UseSkillCoroutine(target));
        StopCoroutine(coroutine);

        _animation.IdleAnimate();
    }

    private void SkillExcute(List<CEntityRuntimeData> targets)
    {
        Coroutine[] coroutines = new Coroutine[targets.Count];

        for (int i = 0; i < targets.Count; i++)
        {
            coroutines[i] = StartCoroutine(UseSkillCoroutine (targets[i]));
        }

        for (int i = 0; i < targets.Count; i++)
        {
            StopCoroutine(coroutines[i]);

            _animation.IdleAnimate(targets[i].GetAnimator());
        }
    }

    private IEnumerator UseSkillCoroutine(CEntityRuntimeData target)
    {
        bool critical = CheckCritical(target);
        bool hit = CheckHit(target);

        _skillData.UseSKill(_entity, target, critical, hit);

        // 여기 앞이나 뒤에 연출 코드를 넣으시면 됩니다
        // 만약 코루틴으로 연출을 넣으실 땐 CSkillDataSO의 Delay를 참조해서 사용해주세요
        // 애니메이션이 해당 Delay를 참조해서 해당 시간동안 작동하기 때문에 해당 Delay를 참조하면 동일한 시간동안 움직일 수 있습니다
        // 추천하는 형태는 UseSkillAnimationCoroutine F12로 따라가셔서 확인해보시면 될 것 같습니다

        Coroutine animationA = StartCoroutine(UseSkillCoroutine(target));
        Coroutine targetAnimation = hit ? StartCoroutine(_animation.HitAnimationCoroutine(target.GetAnimator(), _skillData.UseSkillDelay, _skillData.AfterSkillDelay)) : StartCoroutine(_animation.DodgeAnimationCoroutine(target.GetAnimator(), _skillData.UseSkillDelay, _skillData.AfterSkillDelay));

        //yield return animationA;
        yield return targetAnimation;
    }
    
    private void OffIndicator()
    {
        if(_teamRanks != null)
        {
            for(int i = 0; i < _teamRanks.Length; i++)
            {
                _teamRanks[i].ResetTargetRank();
            }
        }

        if(_enemyRanks != null)
        {
            for(int i = 0; i < _enemyRanks.Length; i++)
            {
                _enemyRanks[i].ResetTargetRank();
            }
        }

        if (_teamPlusIcons != null)
        {
            for (int i = 0; i < _teamPlusIcons.Length; i++)
            {
                _teamPlusIcons[i].SetActive(false);
            }
        }

        if (_enemyPlusIcons != null)
        {
            for (int i = 0; i < _enemyPlusIcons.Length; i++)
            {
                _enemyPlusIcons[i].SetActive(false);
            }
        }

    }

}
