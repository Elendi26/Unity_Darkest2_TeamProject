using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

[CreateAssetMenu(fileName = "TestSkillData", menuName = "TestSkillSO/testSkill")]
public class DummySkillDataSO : ScriptableObject
{
    [Header("스킬 고유 ID")]
    [SerializeField] private string _skillID;

    [Header("스킬 이름")]
    [SerializeField] private string _skillName;

    [Header("스킬 설명")]
    [SerializeField] private string _skillExplain;

    [Header("스킬 원거리 or 근거리 타입")]
    [SerializeField] private string _skillType;

    [Header("스킬 세부 타입(피해 주는 스킬, 회복 스킬 등")]
    [SerializeField] private string _skillDetail;

    [Header("스킬 아이콘 이미지")]
    [SerializeField] private Sprite _skillIcon;

    [Header("스킬 근거리 or 원거리 이미지")]
    [SerializeField] private Sprite _skillTypeIcon;

    [Header("스킬 사용 가능 자리 및 공격 가능 자리 이미지")]
    [SerializeField] private Sprite _skillRangeImage;


    // 프로퍼티

    // 일단 테스트용으로
    public string SkillID => _skillIcon.name;
    public string SkillName => _skillName;
    public string SkillExplain => _skillExplain;
    public string SkillType => _skillType;
    public string SkillDetail => _skillDetail;

    public Sprite SkillIcon => _skillIcon;
    public Sprite SkillTypeIcon => _skillTypeIcon;
    public Sprite SkillRangeImage => _skillRangeImage; 





}
