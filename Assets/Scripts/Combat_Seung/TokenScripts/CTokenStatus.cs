using System.Collections.Generic;
using UnityEngine;


// 단순히 토큰을 보유 및 스택 관리 등을 담당하는 클래스
// 토큰의 상호작용 처리는 다른 클래스에서...

[System.Flags]
public enum ETokenBitMask
{
    None = 0,
    //
    Crit = 1 << 0,
    // 크리티컬 : 적에게 피해를 입히거나 지속 피해/ 약화 효과를 가진 기술/전투 아이템 사용, 아군/자신에게 치유 효과의 기술/전투 아이템 사용 (타겟팅함)
    // 판정 : 기술 사용 시 (빗나감 시에도 토큰 소모됨)
    Riposte = 1 << 1,
    // 피해를 입히는 기술 맞음 / 단순 약화 효과는 적용 X (타겟팅됨)
    // 판정 : 피해를 입히는 기술의 대상이 될 시 (빗나감 시에도 토큰 소모됨)
    Dodge = 1 << 2,
    DodgeE = 1 << 3,
    // 적의 공격을 받음 (타겟팅됨)
    // 판정 : 피해를 입히는 기술의 대상이 될 시 (빗나가지 않아도 토큰 소모)
    Block = 1 << 4,
    BlockE = 1 << 5,
    // 적의 공격으로 피해를 입음 (타겟팅됨)
    // 판정 : 피해를 입었을 때 (빗나가지 않았을 때만), 강화 보호 (BlockE)가 먼저 소모됨
    Guard = 1 << 6,
    Guarded = 1 << 7,
    // Guard를 보유한 대상이 Guarded를 대상으로한 공격을 대신 맞음 (Guard만 타겟팅됨)
    // 판정 : Guarded를 보유한 대상만이 공격 대상이 됐을 때 (Guard도 함께 맞으면 소모되지 않고 대신 맞지도 않음)
    Strength = 1 << 8,
    // 적에게 피해를 입히는 기술/전투아이템 사용 (타겟팅함)
    // 판정 : 피해를 입히는 기술 사용 시 (피해를 입히지 못해도 소모)
    Speed = 1 << 9,
    // 라운드 시작 시
    // 판정 : 라운드 시작 시 소모됨 (속도 100 증가)
    Stealth = 1 << 10,
    // 은신을 얻은 다음 턴 종료 시
    // 판정 : 토큰을 얻은 후 턴 종료 시
    Daze = 1 << 11,
    // 해당 라운드 턴 마지막으로 밀림
    // 라운드 시작 후 속도 굴림 => 속도가 빠른 순으로 다시 턴 맨 뒤로 배치
    Stun = 1 << 12,
    // 해당 라운드 행동을 상실
    // Daze와 공존 불가, 이름은 다르지만 DazeE 와 유사하다고 보면 됨
    Blind = 1 << 13,
    // 적을 대상으로 하는 기술/아이템 사용 (타겟팅함)
    // 판정 : 적을 대상으로하는 기술 사용
    Weak = 1 << 14,
    // 적에게 피해를 입힐 시 (타겟팅함)
    // 판정 : 피해를 입혔을때만 (빗나가면 소모되지 않음)
    Vulnerable = 1 << 15,
    // 적에게 피해를 입었을 때 (타겟팅됨)
    // 판정 : 피해를 입었을 때만 (0도 포함) (빗나가면 소모되지 않음)
    Taunt = 1 << 16,
    // 적의 공격을 받았을 때 (타겟팅됨)
    // 판정 : 광역 공격도 도발 대상을 포함하면 사용가능
    Immobilize = 1 << 17,
    // 턴 종료 시
    // 이동할 수 없음, 이동 배열에 포함되지 않음
    Combo = 1 << 18
    // 연계를 소모하면 (보통 타겟팅됨)
    // 판정 : 연계를 소모하는 기술의 대상이 될 시
}

public class CTokenStatus : MonoBehaviour
{

    private ETokenBitMask _tokenStatus = ETokenBitMask.None;
    private List<STokenData> _tokenData;


    /// <summary>
    /// Resources 사용 중!!!
    /// </summary>
    private void Awake()
    {
        CTokenStructDataSO tokenStructData = Resources.Load<CTokenStructDataSO>("TokenDataSO/TokenStructData");

        if (tokenStructData != null)
        {
            Debug.Log("TokenStructData 로드 성공");

            _tokenData = tokenStructData.TokenStructMaker();
        }
        else
        {
            Debug.LogWarning("TokenStructData 로드 실패! 확인 요망");
        }
    }

    public void AddToken(params ETokenBitMask[] tokens)
    {
        foreach (var token in tokens)
        {
            _tokenStatus |= token;
        }
    }

    public void RemoveToken(params ETokenBitMask[] tokens)
    {
        foreach (var token in tokens)
        {
            _tokenStatus &= ~token;
        }
    }


}
