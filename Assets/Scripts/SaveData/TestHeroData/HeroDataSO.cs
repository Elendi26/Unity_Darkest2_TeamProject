using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "New Hero", menuName = "Darkest Dungeon/Hero Data")]
public class HeroDataSO : ScriptableObject
{
    // 데이터 저장을 위한 고유 ID
    [SerializeField] private string _saveID = "Test_HighWayMan";

    #region 인스펙터
    [Header("직업 이름")]
	[SerializeField] private string _jobTitle;

	[Header("영웅 이름")]
	[SerializeField] private string _heroName;

	[Header("영웅 한마디")]
	[SerializeField] private string _heroSmallTalk;

	[Header("영웅 특성")]
	[SerializeField] private string[] _heroSpecs;

	[Header("영웅 체력")]
	[SerializeField] private int _heroHP;
	[Header("영웅 속도")]
	[SerializeField] private int _heroSpeed;
	[Header("영웅 출혈 저항력")]
	[SerializeField] private int _resBleeding;
	[Header("영웅 중독 저항력")]
	[SerializeField] private int _resPoisoning;
    [Header("영웅 화상 저항력")]
    [SerializeField] private int _resBurn;
    [Header("영웅 기절 저항력")]
    [SerializeField] private int _resStun;
    [Header("영웅 이동 저항력")]
    [SerializeField] private int _resMove;
    [Header("영웅 약화 저항력")]
    [SerializeField] private int _resWeakening;
    [Header("영웅 질병 저항력")]
    [SerializeField] private int _resDisease;
    [Header("영웅 죽격 저항력")]
    [SerializeField] private int _resDeathBlow;
    [Header("영웅 스트레스 수치")]
    [SerializeField] private int _heroStressValue;

	[Header("영웅 초상화")]
	[SerializeField] private Sprite _heroPortrait;

	[Header("위치 및 대상 표시 이미지")]
	[SerializeField] private Sprite _heroRank;

	[Header("영웅 스킬 이미지")]
	[SerializeField] private Sprite[] _heroSkills;

	[Header("영웅 3D 모델")]
	[SerializeField] private GameObject _heroModel;

	#endregion

	// 프로퍼티

	public string SaveID => _saveID;

	public string JobTitle => _jobTitle;
	public string HeroName => _heroName;
	public string HeroSmallTalk => _heroSmallTalk;
	public string[] HeroSpecs => _heroSpecs;

	public int HeroStressValue => _heroStressValue;
	public int HeroSpeed => _heroSpeed;
	public int HeroHP => _heroHP;
	public int ResBleeding => _resBleeding;
	public int ResStun => _resStun;
	public int ResMove => _resMove;
	public int ResBurn => _resBurn;
	public int ResPoisoning => _resPoisoning;
	public int ResWeakening => _resWeakening;
	public int ResDisease => _resDisease;
	public int ResDeathBlow => _resDeathBlow;

	public Sprite HeroPortrait => _heroPortrait;
	public Sprite HeroRank => _heroRank;
	public Sprite[] HeroSkills => _heroSkills;
	public GameObject HeroModel => _heroModel;
	

}
