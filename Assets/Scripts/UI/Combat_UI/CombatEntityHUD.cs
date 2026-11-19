using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatEntityHUD : MonoBehaviour
{
    #region 인스펙터
    [Header("현재 라운드 남은 차례 강조 인디케이터 UI")]
    [SerializeField] private GameObject _turnIndicator;

    [Header("체력 게이지")]
    [SerializeField] private Image _hpBar;

    [Header("스트레스 바")]
    [SerializeField] private Image[] _stressBar = new Image[10];

    [Header("토큰 창")]
    [SerializeField] private GameObject _tokenBox;

    #endregion

    // 내부 변수



    void Start()
    {
        
    }

    /// <summary>
    /// HUD 갱신 (체력, 스트레스, 턴 표시바, 토큰창 리셋 및 띄우기)
    /// </summary>
    /// <param name="data"></param>
    public void RenewHUD(CEntityRuntimeData data)
    {
        float hpGauge = data.GetStat(EEntityStat.HP) / data.GetStat(EEntityStat.MaxHP);

        if (hpGauge <= 0)
        {
            hpGauge = 0;
        }

        _hpBar.fillAmount = hpGauge;

        // 아군 영웅이 아닐때 (적일때 스트레스 칸 비활성화)
        if(!data.IsHero())
        {
            if(_stressBar != null)
            {
                for (int i = 0; i < _stressBar.Length; i++)
                {
                    _stressBar[i].gameObject.SetActive(false);
                }
            }

            if(_tokenBox != null)
            {
                _tokenBox.SetActive(false);
               
            }

            // 다 끄고 함수 종료하기
            return;
        }

        // 스트레스바 세팅
        float stressValue = data.GetStat(EEntityStat.Stress);
        int currentStress = Mathf.RoundToInt(stressValue);

        // 현재 스트레스 수치까지 반복문 돌려서 게이지 한칸씩 켜기
        if(currentStress != 0)
        {
            for(int i = 0; i < _stressBar.Length; i++)
            {
               _stressBar[i].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 피해를 당했을 때, 회복했을 때 등 체력에 관여하는 모든 이벤트 발생시 hp바를 갱신하는 함수 
    /// </summary>
    public void RenewHp()
    {

    }


}
