using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroPopup_UI : MonoBehaviour
{
    // 이 팝업창의 경우 역마차, 전투, 갈림길 등 어느 곳에서나 켜지기 때문에 싱글톤으로 어디서나 부를 수 있게 한다.
    public static HeroPopup_UI instance;

    #region 인스펙터
    [Header("직업 이름이 들어갈 텍스트")]
    [SerializeField] private TextMeshProUGUI _jopTitle;

    [Header("영웅 이름이 들어갈 텍스트")]
    [SerializeField] private TextMeshProUGUI _heroName;

    [Header("캐릭터 모델 띄울 이미지 공간")]
    [SerializeField] private RawImage _heroModelSpace;

    [Header("전투 기술이 들어갈 이미지 공간")]
    [SerializeField] private GameObject[] _combatSkills;

    // TODO : 기벽, 관계, 상태 페이지에 들어갈 변수

    // 저항력 수치 들어갈 텍스트
    [Header("출혈")]
    [SerializeField] private TextMeshProUGUI _bleedingValue;
    [Header("중독")]
    [SerializeField] private TextMeshProUGUI _poisoningValue;
    [Header("화상")]
    [SerializeField] private TextMeshProUGUI _burnValue;
    [Header("기절")]
    [SerializeField] private TextMeshProUGUI _stunValue;
    [Header("이동")]
    [SerializeField] private TextMeshProUGUI _moveValue;
    [Header("약화")]
    [SerializeField] private TextMeshProUGUI _weakeningValue;
    [Header("질병")]
    [SerializeField] private TextMeshProUGUI _diseaseValue;
    [Header("죽격")]
    [SerializeField] private TextMeshProUGUI _deathblowValue;
    [Header("스트")]
    [SerializeField] private TextMeshProUGUI _stressValue;
    [Header("체력")]
    [SerializeField] private TextMeshProUGUI _heroHP;
    [Header("속도")]
    [SerializeField] private TextMeshProUGUI _speed;

    // TODO : 장신구 들어갈 배열 or 리스트 변수

    #endregion

    // 내부 변수

    private int _currentHP;


    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(this);

    }

    public void HeroDataUpdate(HeroDataSO data)
    {
        if(data == null)
        {
            return;
        }

        _jopTitle.text = data.JobTitle;
        _heroName.text = data.HeroName;
        _bleedingValue.text = data.ResBleeding.ToString();
        _poisoningValue.text = data.ResPoisoning.ToString();
        _burnValue.text = data.ResBurn.ToString();
        _stunValue.text = data.ResStun.ToString();
        _moveValue.text = data.ResMove.ToString();
        _weakeningValue.text = data.ResWeakening.ToString();
        _diseaseValue.text = data.ResDisease.ToString();
        _deathblowValue.text = data.ResDeathBlow.ToString();
        _stressValue.text = data.HeroStressValue.ToString();
        _heroHP.text = data.HeroHP.ToString();
        _speed.text = data.HeroSpeed.ToString();

        // 둘 중 더 작은 값을 기준으로 반복문 돌릴 수 있게
        int numA = Mathf.Min(data.HeroSkills.Length, _combatSkills.Length);

        for (int i = 0; i < numA; i++)
        {
            _combatSkills[i].GetComponent<Image>().sprite = data.HeroSkills[i];
        }

    }

}
