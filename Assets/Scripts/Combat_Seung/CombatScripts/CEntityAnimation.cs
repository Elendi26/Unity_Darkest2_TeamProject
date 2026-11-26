using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
선택 시 antic 호출 -> idle 호출 (스킬 선택 후 대기 상태 애니메이션) 이후 스킬 사용 -> 복귀 애니메이션 -> idle

피격 시 피격 애니메이션 hit reaction -> hit recovery
*/

public class CEntityAnimation : MonoBehaviour
{
    private Animator _animator;
    private string _skillName;
    private ESkillType _skillType;
    private float _useSkillDelay; // 스킬 애니메이션 딜레이
    private float _afterSkillDelay; // 스킬 종료 후 복귀 딜레이

    public void InputAnimationData(Animator animator, CSkillDataSO skillData)
    {
        _animator = animator;
        _skillName = skillData.SkillName;
        _skillType = skillData.Type;
        _useSkillDelay = skillData.UseSkillDelay;
        _afterSkillDelay = skillData.AfterSkillDelay;
    }

    public void AnticAnimation()
    {
        switch (_skillType)
        {
            case ESkillType.Melee:

                _animator.SetTrigger("Antic_Melee");

                break;

            case ESkillType.Ranged:

                _animator.SetTrigger("Antic_Ranged");

                break;

            case ESkillType.Buff:

                _animator.SetTrigger("Antic_Buff");

                break;

            case ESkillType.Heal:

                _animator.SetTrigger("Antic_Buff");

                break;

            default:

                break;
        }
    }

    public IEnumerator UseSkillAnimationCoroutine()
    {
        Debug.Log("스킬 애니메이션 발동");

        _animator.SetTrigger("UseSKill");

        yield return new WaitForSeconds(_useSkillDelay);

        _animator.SetTrigger($"Skill_Recovery");

        yield return new WaitForSeconds(_afterSkillDelay);
    }

    public IEnumerator HitAnimationCoroutine(Animator target, float useDelay, float afterDelay)
    {
        target.SetTrigger("Hit");

        yield return new WaitForSeconds(useDelay);

        target.SetTrigger($"Hit_Recovery");

        yield return new WaitForSeconds(afterDelay);
    }

    public IEnumerator DodgeAnimationCoroutine(Animator target, float useDelay, float afterDelay)
    {
        target.SetTrigger("Dodge");

        yield return new WaitForSeconds(useDelay);

        target.SetTrigger($"Dodge_Recovery");

        yield return new WaitForSeconds(afterDelay);
    }

    public void MoveAnimation()
    {
        _animator.SetTrigger("Move");
    }

    public void IdleAnimate()
    {
        _animator.SetTrigger("Idle");
    }

    public void IdleAnimate(Animator target)
    {
        _animator.SetTrigger("Idle");
    }
}
