using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CEntitySkillAnimation : MonoBehaviour
{
    private Animator _animator;
    private string _skillName;
    private float _usingSkillDelay; // 스킬 애니메이션 딜레이
    private float _finishDelay; // 스킬 종료 후 복귀 딜레이

    public CEntitySkillAnimation (Animator animator, string skillName, float usingSkill = 1.08f, float finish = 1.08f)
    {
        _animator = animator;
        _skillName = skillName;
        _usingSkillDelay = usingSkill;
        _finishDelay = finish;
    }

    public IEnumerator UsingSkillCoroutine()
    {
        Debug.Log("스킬 애니메이션 발동");

        _animator.SetTrigger($"{_skillName}_UsingSkill");

        yield return new WaitForSeconds(_usingSkillDelay);

        yield return StartCoroutine(FinishCoroutine());
    }

    public IEnumerator FinishCoroutine()
    {
        Debug.Log("스킬 종료 애니메이션");

        _animator.SetTrigger($"{_skillName}_Finish");

        yield return new WaitForSeconds(_finishDelay);
    }


    public void SetIdleAnimate()
    {
        _animator.SetTrigger("Idle");
    }

    public void PrepareAnimate()
    {
        _animator.SetTrigger($"{_skillName}_Prepare");
    }

}
