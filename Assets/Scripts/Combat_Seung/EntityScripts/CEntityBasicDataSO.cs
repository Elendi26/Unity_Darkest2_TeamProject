using System.Collections.Generic;
using UnityEngine;

/*
각 엔티티들 초기 능력치

이름/최대체력/속도/각 저항치/이동가능거리
*/

[CreateAssetMenu(menuName = "2D3D/SO/HeroBasicDataSO", fileName = "HeroBasicData_")]
public class CEntityBasicDataSO : ScriptableObject
{
    #region 인스펙터

    [Header("영웅 고유 ID")]
    [SerializeField] private string _heroID = "Test";

    [Header("적/영웅 이름")]
	[SerializeField] private string _name;
    [Header("직업 이름")]
    [SerializeField] private string _jobTitle;
    [Header("영웅 한마디")]
    [SerializeField] private string _heroSmallTalk;
    [Header("영웅 특성")]
    [SerializeField] private string[] _heroSpecs;

    [Header("체력 및 속도")]
	[SerializeField, Min(0)] private int _maxHP;
    [SerializeField, Min(0)] private int _currentHP;
    [SerializeField, Min(0)] private int _maxStress = 10;
	[SerializeField, Min(0)] private int _speed;

	[Header("저항치")]
	[SerializeField, Min(0)] private float _bleedRes;
	[SerializeField, Min(0)] private float _blightRes;
	[SerializeField, Min(0)] private float _burnRes;
	[SerializeField, Min(0)] private float _diseaseRes;
	[SerializeField, Min(0)] private float _stunRes;
	[SerializeField, Min(0)] private float _moveRes;
	[SerializeField, Min(0)] private float _debuffRes;
	[SerializeField, Min(0)] private float _deathblowRes;
    [SerializeField, Min(0)] private float _currentStress;

    [Header("영웅/적 여부")]
    [SerializeField] private bool _hero;

    [Header("이동 가능 거리 / 적일 경우 사이즈")]
	[SerializeField, Min(0)] private int _movement;

    [Header("영웅 스킬 5개 배치")]
    [SerializeField] private List<CSkillDataSO> _heroSkills = new List<CSkillDataSO>();

    [Header("영웅 3D 모델")]
    [SerializeField] private GameObject _heroModel;

    [Header("영웅 초상화")]
    [SerializeField] private Sprite _heroPortrait;

    [Header("위치 및 대상 표시 이미지")]
    [SerializeField] private Sprite _heroRank;

    [Header("애니메이터")]
    [SerializeField] private Animator _animator;

    #endregion

    #region 프로퍼티
    public string HeroID => _heroID;

    public string Name => _name;
    public string JobTitle => _jobTitle;
    public string HeroSmallTalk => _heroSmallTalk;
    public string[] HeroSpecs => _heroSpecs;
    //
    public int MaxHP => _maxHP;
    public int CurrentHP => _currentHP;
    public int Speed => _speed;
    public int MaxStress => _maxStress;

	//
    public float BleedRes => _bleedRes;
    public float BlightRes => _blightRes;
    public float BurnRes => _burnRes;
    public float DiseaseRes => _diseaseRes;
    public float StunRes => _stunRes;
    public float MoveRes => _moveRes;
    public float DebuffRes => _debuffRes;
    public float DeathblowRes => _deathblowRes;
    public float CurrentStress => _currentStress;
    //

    public bool Hero => _hero;

    //
    public int Movement => _movement;
    public Sprite HeroPortrait => _heroPortrait;
    public Sprite HeroRank => _heroRank;
    public List<CSkillDataSO> HeroSkills => _heroSkills;
    public GameObject HeroModel => _heroModel;

    public Animator Animator => _animator;

    #endregion

}
