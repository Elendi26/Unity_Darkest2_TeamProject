using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class UserProfileData
{
    // 유저가 지은 프로필 이름 (게임 기본은 Darkest)
    public string ProfileName;

    // 데이터를 식별하기 위한 아이디
    public string ProfileID;

    // 마지막 데이터 저장 시간
    public string LastSaveTime;

    // 보유한 촛불 개수
    public int CandleCount;


    // TODO : 맵 진행도, 노드 진행도, 역마차 상태, 영웅 데이터 변수 등 추가해야한다.


    // TODO : 관련 클래스에서 데이터를 저장하는 함수를 만들어야한다.
    [System.Serializable]
    public class NodeSaveData
    {

    }

    [System.Serializable]
    public class HeroSaveData
    {
        public string HeroID;
        public int CurrentHP;
        public int CurrentStress;
        public int PartySlotIndex;

        // TODO : 영웅 간 호감도 수치 상대 영웅 ID와 수치를 받아서 Dictionary로 적용

        // 영웅이 전투시 들고있는 스킬 데이터 ID 리스트 / 리스트의 들어간 순서대로 스킬창에 왼쪽부터 표시된다.
        public List<string> HeroSkillIDs = new List<string>();



    }

    // 파티원들hdata.PartyHeroes.Count의 동적 데이터를 담기 위한 리스트 변수
    public List<HeroSaveData> PartyHeroes = new List<HeroSaveData>();

    // 튜토리얼을 마쳤는지 (첫 모험이면 튜토리얼부터 시작)
    public bool TutorialDone;

    // 진행 중인 런 (역마차 자원, 짐칸, 장착품, 이어할 씬). 없으면 null
    public CRunSaveData Run;

}
